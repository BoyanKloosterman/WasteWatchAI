from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from datetime import datetime, date
import pandas as pd
import numpy as np
import pickle
import os
from typing import Dict, Any
from sklearn.ensemble import RandomForestRegressor
from sklearn.tree import DecisionTreeRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import r2_score
import requests
import warnings

# Suppress the specific sklearn warning
warnings.filterwarnings("ignore", message="X has feature names, but DecisionTreeRegressor was fitted without feature names")

# Maak FastAPI app EERST - net zoals in main.py
app = FastAPI(title="Waste Prediction API", version="1.0.0")

# CORS middleware - exact zoals in main.py
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global variables voor models
dt_models = {}
rf_models = {}
dt_r2_scores = {}
rf_r2_scores = {}
available_features = []
weather_mapping = {}

class PredictionRequest(BaseModel):
    datum: str  # Format: "YYYY-MM-DD"
    temperatuur: float
    weersverwachting: str
    latitude: float = 51.5865  # Default voor Breda centrum
    longitude: float = 4.7761  # Default voor Breda centrum

class PredictionResponse(BaseModel):
    datum: str
    predictions: Dict[str, int]
    confidence_scores: Dict[str, float]
    model_used_per_category: Dict[str, str] = {}  # NIEUW
    input_parameters: Dict[str, Any]
    data_source: str

def load_data_and_train_models():
    """Load data and train both Decision Tree and Random Forest models"""
    global dt_models, rf_models, dt_r2_scores, rf_r2_scores, available_features, weather_mapping
    
    # API URLs
    trashUrl = "http://localhost:8080/api/TrashItems/dummy"
    weerUrl = "http://localhost:8080/api/Weather/"
    
    try:
        print("Loading trash data...")
        # Load trash data
        trash = pd.read_json(trashUrl)
        print(f"Loaded {len(trash)} trash records")
        
        # Convert timestamp to datetime
        trash['timestamp'] = pd.to_datetime(trash['timestamp'])
        trash['datum'] = trash['timestamp'].dt.date
        trash['hour'] = trash['timestamp'].dt.hour
        trash['min'] = trash['timestamp'].dt.minute
        trash['year'] = trash['timestamp'].dt.year
        trash['month'] = trash['timestamp'].dt.month
        trash['day'] = trash['timestamp'].dt.day
        trash['weekday'] = trash['timestamp'].dt.weekday
        
        # Try to get weather data
        try:
            print("Fetching weather data...")
            weather_response = requests.get(weerUrl, timeout=10)
            if weather_response.status_code == 200:
                weather_data = weather_response.json()
                weather_df = pd.DataFrame(weather_data)
                print(f"Loaded {len(weather_df)} weather records")
                
                if 'timestamp' in weather_df.columns:
                    weather_df['datum'] = pd.to_datetime(weather_df['timestamp']).dt.date
                    weather_df = weather_df.rename(columns={
                        'temperature': 'temperatuur',
                        'weatherDescription': 'weersverwachting'
                    })
                elif 'Timestamp' in weather_df.columns:
                    weather_df['datum'] = pd.to_datetime(weather_df['Timestamp']).dt.date
                    weather_df = weather_df.rename(columns={
                        'Temperature': 'temperatuur',
                        'WeatherDescription': 'weersverwachting'
                    })
                
                # Merge weather data
                trash = pd.merge(trash, weather_df[['datum', 'temperatuur', 'weersverwachting']], 
                               on='datum', how='left')
                print("Weather data merged successfully")
        except Exception as e:
            print(f"Weather API failed: {e}. Using dummy weather data.")
            # Use dummy weather data if API fails
            trash['temperatuur'] = 15.0
            trash['weersverwachting'] = 'Bewolkt'
        
        # Fill missing weather data
        trash['temperatuur'] = trash['temperatuur'].fillna(15.0)
        trash['weersverwachting'] = trash['weersverwachting'].fillna('Bewolkt')
        
        # Create daily dataset with categories
        if 'litterType' in trash.columns:
            daily_waste_by_category = trash.groupby(['datum', 'litterType']).size().unstack(fill_value=0)
            
            # Map categories to standardized names
            category_mapping = {}
            for col in daily_waste_by_category.columns:
                col_lower = col.lower()
                if 'plastic' in col_lower:
                    category_mapping[col] = 'Plastic'
                elif 'paper' in col_lower or 'papier' in col_lower:
                    category_mapping[col] = 'Papier'
                elif 'organic' in col_lower or 'organisch' in col_lower or 'bio' in col_lower:
                    category_mapping[col] = 'Organisch'
                elif 'glass' in col_lower or 'glas' in col_lower:
                    category_mapping[col] = 'Glas'
                else:
                    category_mapping[col] = 'Overig'
            
            daily_waste_by_category = daily_waste_by_category.rename(columns=category_mapping)
            
            # Combine duplicate categories
            if len(set(category_mapping.values())) < len(category_mapping):
                daily_waste_by_category = daily_waste_by_category.groupby(level=0, axis=1).sum()
            
            expected_categories = ['Plastic', 'Papier', 'Organisch', 'Glas']
            for cat in expected_categories:
                if cat not in daily_waste_by_category.columns:
                    daily_waste_by_category[cat] = 0
            
            daily_waste_by_category = daily_waste_by_category[expected_categories]
            # Voeg totaal_afval toe
            daily_waste_by_category['totaal_afval'] = daily_waste_by_category.sum(axis=1)
        else:
            daily_waste_by_category = pd.DataFrame()
            expected_categories = ['Plastic', 'Papier', 'Organisch', 'Glas']
            for cat in expected_categories:
                daily_waste_by_category[cat] = 0
            daily_waste_by_category['totaal_afval'] = 0
        
        # Create aggregation dictionary
        agg_dict = {
            'latitude': 'median',
            'longitude': 'median',
            'year': 'first',
            'month': 'first',
            'day': 'first',
            'weekday': 'first',
            'hour': 'mean',
            'temperatuur': 'mean',
            'weersverwachting': 'first'
        }
        
        # Aggregate features per day
        daily_features = trash.groupby('datum').agg(agg_dict).reset_index()
        
        # Weather mapping
        weather_mapping = {
            'Zonnig': 0,
            'Gedeeltelijk bewolkt': 1,
            'Bewolkt': 2,
            'Regenachtig': 3,
            'Regen': 3,
            'Lichte motregen': 3,
            'Motregen': 3,
            'Onweer': 4,
            'Sneeuw': 5,
            'Mistig': 6,
            'Onbekend': 1
        }
        
        daily_features['weersverwachting_num'] = daily_features['weersverwachting'].map(weather_mapping).fillna(1)
        
        # Combine features with waste counts
        daily_data = pd.merge(daily_features, daily_waste_by_category.reset_index(), on='datum')
        
        # Add extra features
        daily_data['is_weekend'] = (daily_data['weekday'] >= 5).astype(int)
        daily_data['seizoen'] = daily_data['month'].map({
            12: 0, 1: 0, 2: 0,  # Winter
            3: 1, 4: 1, 5: 1,    # Lente  
            6: 2, 7: 2, 8: 2,    # Zomer
            9: 3, 10: 3, 11: 3   # Herfst
        })
        
        # Define features
        features = ['latitude', 'longitude', 'year', 'month', 'day', 'weekday',
                   'temperatuur', 'weersverwachting_num', 'is_weekend', 'seizoen']
        
        available_features = [f for f in features if f in daily_data.columns]
        print(f"Available features: {available_features}")
        
        # Train models for each target (inclusief totaal_afval)
        targets = ['Plastic', 'Papier', 'Organisch', 'Glas']
        
        print("Training models...")
        for target_name in targets:
            if target_name not in daily_data.columns:
                print(f"Warning: Target {target_name} not found in data")
                continue
                
            target = daily_data[target_name]
            
            # Skip if target has no variance
            if target.std() == 0:
                print(f"Warning: Target {target_name} has no variance, skipping")
                continue
            
            # Train-test split - gebruik numpy arrays om feature name warnings te vermijden
            X_train, X_test, y_train, y_test = train_test_split(
                daily_data[available_features].values,  # Use .values to get numpy array
                target.values,  # Use .values to get numpy array
                test_size=0.3, random_state=42
            )
            
            # Train Decision Tree
            dt = DecisionTreeRegressor(max_depth=8, random_state=42,)
            dt.fit(X_train, y_train)
            dt_pred = dt.predict(X_test)
            dt_r2 = r2_score(y_test, dt_pred)
            
            # Calculate Decision Tree confidence on test set
            dt_confidence_scores = []
            for i in range(min(10, len(X_test))):  # Sample van 10 voor snelheid
                single_input = X_test[i:i+1]  # Numpy array slice
                dt_conf = calculate_prediction_confidence(dt, single_input, target_name)
                dt_confidence_scores.append(dt_conf)
            dt_avg_confidence = np.mean(dt_confidence_scores)

            # Train Random Forest
            rf = RandomForestRegressor(n_estimators=1000, max_depth=5, random_state=42, )
            rf.fit(X_train, y_train)
            rf_pred = rf.predict(X_test)
            rf_r2 = r2_score(y_test, rf_pred)
            
            # Calculate Random Forest confidence on test set
            rf_confidence_scores = []
            for i in range(min(10, len(X_test))):  # Sample van 10 voor snelheid
                single_input = X_test[i:i+1]  # Numpy array slice
                rf_conf = calculate_prediction_confidence(rf, single_input, target_name)
                rf_confidence_scores.append(rf_conf)
            rf_avg_confidence = np.mean(rf_confidence_scores)
            
            # Store models and scores
            dt_models[target_name] = dt
            rf_models[target_name] = rf
            dt_r2_scores[target_name] = max(0, dt_r2)
            rf_r2_scores[target_name] = max(0, rf_r2)
            
            print(f"  {target_name}:")
            print(f"    Decision Tree:  R²={dt_r2:.3f}, Avg Confidence={dt_avg_confidence:.3f}")
            print(f"    Random Forest:  R²={rf_r2:.3f}, Avg Confidence={rf_avg_confidence:.3f}")
        
        print(f"Successfully trained models for {len(dt_models)} targets")
        return True
        
    except Exception as e:
        print(f"Error loading data and training models: {e}")
        import traceback
        traceback.print_exc()
        return False

def calculate_prediction_confidence(model, input_data, target_name):
    """Calculate confidence score for a prediction"""
    try:
        if hasattr(model, 'estimators_'):
            # Voor Random Forest: gebruik std van alle trees
            predictions = np.array([tree.predict(input_data)[0] for tree in model.estimators_])
            mean_pred = np.mean(predictions)
            std_pred = np.std(predictions)
            
            # Confidence = 1 - (normalized std)
            max_std = mean_pred * 0.5 if mean_pred > 0 else 1.0
            normalized_std = min(std_pred / max_std, 1.0) if max_std > 0 else 0.0
            confidence = max(0.0, 1.0 - normalized_std)
            
            return round(confidence, 3)
        else:
            # Voor Decision Tree: gebruik feature importance
            feature_importances = model.feature_importances_
            max_importance = np.max(feature_importances)
            
            # Eenvoudige confidence gebaseerd op max feature importance
            confidence = min(max_importance * 2, 1.0)
            
            return round(confidence, 3)
            
    except Exception as e:
        print(f"Error calculating confidence for {target_name}: {e}")
        return 0.5  # Default moderate confidence

def get_best_model_type():
    """Determine which model type has better average R² score"""
    if not dt_r2_scores or not rf_r2_scores:
        return "random_forest"  # Default
    
    dt_avg_r2 = np.mean(list(dt_r2_scores.values()))
    rf_avg_r2 = np.mean(list(rf_r2_scores.values()))
    
    print(f"Model comparison: DT avg R²={dt_avg_r2:.3f}, RF avg R²={rf_avg_r2:.3f}")
    return "decision_tree" if dt_avg_r2 > rf_avg_r2 else "random_forest"

def predict_waste(date_str: str, latitude: float, longitude: float, 
                 temperatuur: float, weersverwachting: str):
    """Make prediction using the BEST model per category"""
    
    # Convert date string to datetime
    date_obj = datetime.strptime(date_str, '%Y-%m-%d').date()
    
    # Extract date features
    year = date_obj.year
    month = date_obj.month
    day = date_obj.day
    weekday = date_obj.weekday()
    
    # Calculate derived features
    is_weekend = 1 if weekday >= 5 else 0
    seizoen_mapping = {
        12: 0, 1: 0, 2: 0,  # Winter
        3: 1, 4: 1, 5: 1,   # Lente  
        6: 2, 7: 2, 8: 2,   # Zomer
        9: 3, 10: 3, 11: 3  # Herfst
    }
    seizoen = seizoen_mapping[month]
    
    # Map weather description to numeric
    weersverwachting_num = weather_mapping.get(weersverwachting, 1)
    
    # Create input data
    input_values = np.array([[
        latitude, longitude, year, month, day, weekday,
        temperatuur, weersverwachting_num, is_weekend, seizoen
    ]])
    
    # Make predictions - selecteer het BESTE model per categorie
    categories = ['Plastic', 'Papier', 'Organisch', 'Glas']
    predictions = {}
    confidence_scores = {}
    all_model_confidence = {}
    model_used_per_category = {}
    
    for category in categories:
        all_model_confidence[category] = {}
        
        # Bepaal het beste model voor DEZE specifieke categorie
        dt_r2 = dt_r2_scores.get(category, 0)
        rf_r2 = rf_r2_scores.get(category, 0)
        
        # Kies het model met de hoogste R² voor deze categorie
        if dt_r2 > rf_r2 and category in dt_models:
            best_model = dt_models[category]
            model_used_per_category[category] = "decision_tree"
            print(f"Using Decision Tree for {category} (R²={dt_r2:.3f} vs RF R²={rf_r2:.3f})")
        elif category in rf_models:
            best_model = rf_models[category]
            model_used_per_category[category] = "random_forest"
            print(f"Using Random Forest for {category} (R²={rf_r2:.3f} vs DT R²={dt_r2:.3f})")
        else:
            # Fallback
            predictions[category] = 0
            confidence_scores[category] = 0.0
            model_used_per_category[category] = "none"
            continue
        
        # Maak voorspelling met het beste model voor deze categorie
        try:
            prediction = best_model.predict(input_values)[0]
            predictions[category] = max(0, round(prediction))
            
            # Calculate confidence voor het gekozen model
            confidence = calculate_prediction_confidence(best_model, input_values, category)
            confidence_scores[category] = confidence
            
        except Exception as e:
            print(f"Error predicting {category}: {e}")
            predictions[category] = 0
            confidence_scores[category] = 0.0
        
        # Calculate confidence voor beide modellen (voor transparantie)
        try:
            if category in dt_models:
                dt_conf = calculate_prediction_confidence(dt_models[category], input_values, category)
                all_model_confidence[category]['decision_tree'] = dt_conf
            else:
                all_model_confidence[category]['decision_tree'] = 0.0
                
            if category in rf_models:
                rf_conf = calculate_prediction_confidence(rf_models[category], input_values, category)
                all_model_confidence[category]['random_forest'] = rf_conf
            else:
                all_model_confidence[category]['random_forest'] = 0.0
                
        except Exception as e:
            print(f"Error calculating all confidences for {category}: {e}")
            all_model_confidence[category] = {'decision_tree': 0.0, 'random_forest': 0.0}
    
    # Overall model type (meest gebruikte)
    model_types_used = [v for v in model_used_per_category.values() if v != "none"]
    overall_model_used = max(set(model_types_used), key=model_types_used.count) if model_types_used else "mixed"
    
    # Calculate weighted average R² score voor de gekozen modellen
    all_r2_scores = []
    for category in categories:
        if model_used_per_category[category] == "decision_tree":
            all_r2_scores.append(dt_r2_scores.get(category, 0))
        elif model_used_per_category[category] == "random_forest":
            all_r2_scores.append(rf_r2_scores.get(category, 0))
        else:
            all_r2_scores.append(0)
    
    avg_r2_score = np.mean(all_r2_scores) if all_r2_scores else 0
    
    return predictions, confidence_scores, all_model_confidence, overall_model_used, avg_r2_score, model_used_per_category

# Startup function - aangeroepen wanneer server start
def startup_models():
    """Initialize models on startup"""
    print("Starting Waste Prediction API...")
    success = load_data_and_train_models()
    if not success:
        print("Warning: Failed to load data and train models")
    else:
        print("Models loaded successfully!")

# Roep startup functie aan
startup_models()

@app.post("/api/predict", response_model=PredictionResponse)
async def predict_waste_endpoint(request: PredictionRequest):
    """Predict waste amounts using best model per category"""
    try:
        # Validate date format
        datetime.strptime(request.datum, '%Y-%m-%d')
        
        # Check if models are loaded
        if not dt_models and not rf_models:
            raise HTTPException(status_code=503, detail="Models not loaded yet")
        
        # Make prediction with best model per category
        predictions, confidence_scores, all_model_confidence, model_used, avg_r2_score, model_used_per_category = predict_waste(
            request.datum,
            request.latitude,
            request.longitude,
            request.temperatuur,
            request.weersverwachting
        )
        
        return PredictionResponse(
            datum=request.datum,
            predictions=predictions,
            confidence_scores=confidence_scores,
            model_used_per_category=model_used_per_category,
            input_parameters={
                "temperatuur": request.temperatuur,
                "weersverwachting": request.weersverwachting,
                "latitude": request.latitude,
                "longitude": request.longitude
            },
            data_source="TrashItems API + Weather API"
        )
        
    except ValueError as e:
        raise HTTPException(status_code=400, detail=f"Invalid date format. Use YYYY-MM-DD: {str(e)}")
    except Exception as e:
        print(f"Prediction error: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Prediction failed: {str(e)}")
        
    except ValueError as e:
        raise HTTPException(status_code=400, detail=f"Invalid date format. Use YYYY-MM-DD: {str(e)}")
    except Exception as e:
        print(f"Prediction error: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Prediction failed: {str(e)}")

@app.get("/api/model-info")
async def get_model_info():
    """Get information about the trained models"""
    try:
        best_model = get_best_model_type()
        
        # Alle categorieën inclusief totaal_afval
        categories = ['Plastic', 'Papier', 'Organisch', 'Glas', 'totaal_afval']
        dt_category_scores = {k: round(v, 3) for k, v in dt_r2_scores.items() if k in categories}
        rf_category_scores = {k: round(v, 3) for k, v in rf_r2_scores.items() if k in categories}
        
        return {
            "best_model_type": best_model,
            "decision_tree_r2_scores": dt_category_scores,
            "random_forest_r2_scores": rf_category_scores,
            "decision_tree_avg_r2": round(np.mean(list(dt_category_scores.values())), 3) if dt_category_scores else 0,
            "random_forest_avg_r2": round(np.mean(list(rf_category_scores.values())), 3) if rf_category_scores else 0,
            "available_features": available_features,
            "supported_weather_types": list(weather_mapping.keys()),
            "prediction_categories": categories,
            "confidence_info": "Confidence scores range from 0.0 (low) to 1.0 (high confidence)"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to get model info: {str(e)}")

@app.get("/api/health")
async def health_check():
    """Health check endpoint"""
    models_loaded = len(dt_models) > 0 and len(rf_models) > 0
    return {
        "status": "healthy" if models_loaded else "starting",
        "models_loaded": models_loaded,
        "available_targets": len(dt_models),
        "api_version": "1.0.0"
    }

@app.get("/")
def read_root():
    """Root endpoint - net zoals in main.py"""
    return {"message": "Waste Prediction API is running", "version": "1.0.0"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)