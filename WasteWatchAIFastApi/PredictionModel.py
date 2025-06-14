from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from datetime import datetime, date
import pandas as pd
import numpy as np
import pickle
import os
from datetime import datetime, timedelta
from typing import Dict, Any
from sklearn.ensemble import RandomForestRegressor
from sklearn.tree import DecisionTreeRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import r2_score
import requests
import warnings
from fastapi import APIRouter

router = APIRouter()

# Suppress the specific sklearn warning
warnings.filterwarnings("ignore", message="X has feature names, but DecisionTreeRegressor was fitted without feature names")

# Global variables voor models
dt_models = {}
rf_models = {}
dt_r2_scores = {}
rf_r2_scores = {}
available_features = []
weather_mapping = {}

class PredictionRequest(BaseModel):
    datum: str  # Format: "YYYY-MM-DD"
    latitude: float = 51.5890  # Default voor Breda centrum
    longitude: float = 4.7750  # Default voor Breda centrum

class PredictionResponse(BaseModel):
    datum: str
    latitude: float
    longitude: float
    temperatuur: float
    weersverwachting: str
    weather_source: str  # Nieuw: toont bron van weather data
    predictions: Dict[str, int]
    confidence_scores: Dict[str, float]
    model_used_per_category: Dict[str, str] = {}
    data_source: str


def get_weather_from_openmeteo(date_str: str, latitude: float, longitude: float):
    """Haal weer data op van OpenMeteo API voor specifieke datum en locatie"""
    try:
        # Parse datum
        target_date = datetime.strptime(date_str, '%Y-%m-%d').date()
        today = datetime.now().date()
        
        # Bepaal of het historische data of forecast is
        if target_date <= today:
            # Historische data
            api_url = "https://archive-api.open-meteo.com/v1/archive"
        else:
            # Forecast data
            api_url = "https://api.open-meteo.com/v1/forecast"
        
        # API parameters
        params = {
            'latitude': latitude,
            'longitude': longitude,
            'start_date': date_str,
            'end_date': date_str,
            'daily': 'temperature_2m_mean,weather_code',
            'timezone': 'Europe/Amsterdam'
        }
        
        print(f"🌤️ Fetching weather for {date_str} from OpenMeteo...")
        response = requests.get(api_url, params=params, timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            print(f"🔍 OpenMeteo response: {data}")  # Debug log
            
            if 'daily' in data and data['daily']['time']:
                # Pak eerste (en enige) dag
                temperature = data['daily']['temperature_2m_mean'][0] if data['daily']['temperature_2m_mean'] else None
                weather_code = data['daily']['weather_code'][0] if data['daily']['weather_code'] else None
                
                # Valideer dat we echte data hebben
                if temperature is None or weather_code is None:
                    raise Exception(f"OpenMeteo returned None values: temp={temperature}, code={weather_code}")
                
                # Extra validatie voor realistische temperatuur
                if not isinstance(temperature, (int, float)) or temperature < -50 or temperature > 60:
                    raise Exception(f"Invalid temperature value: {temperature}")
                
                # Map weather code to Dutch description
                weather_description = map_weather_code_to_description(weather_code)
                
                print(f"✅ Weather data: {temperature}°C, {weather_description}")
                
                return {
                    'temperatuur': round(float(temperature), 1),
                    'weersverwachting': weather_description,
                    'weather_source': 'OpenMeteo API'
                }
            else:
                raise Exception(f"Invalid OpenMeteo response structure: {data}")
        else:
            raise Exception(f"OpenMeteo API returned status {response.status_code}: {response.text}")
            
    except Exception as e:
        print(f"❌ OpenMeteo API failed: {e}")
        
        # Fallback naar seizoensgemiddelden voor Nederland
        month = datetime.strptime(date_str, '%Y-%m-%d').month
        
        # Nederlandse seizoensgemiddelden
        if month in [12, 1, 2]:  # Winter
            fallback_temp = 5.0
            fallback_weather = 'Bewolkt'
        elif month in [3, 4, 5]:  # Lente
            fallback_temp = 12.0
            fallback_weather = 'Gedeeltelijk bewolkt'
        elif month in [6, 7, 8]:  # Zomer
            fallback_temp = 20.0
            fallback_weather = 'Zonnig'
        else:  # Herfst
            fallback_temp = 12.0
            fallback_weather = 'Regenachtig'
        
        print(f"🔄 Using fallback weather: {fallback_temp}°C, {fallback_weather}")
        
        return {
            'temperatuur': fallback_temp,
            'weersverwachting': fallback_weather,
            'weather_source': 'Fallback (seasonal average)'
        }

def map_weather_code_to_description(weather_code):
    """Map OpenMeteo weather codes to Dutch descriptions"""
    weather_code_mapping = {
        0: 'Zonnig',
        1: 'Gedeeltelijk bewolkt',
        2: 'Gedeeltelijk bewolkt', 
        3: 'Bewolkt',
        45: 'Mistig',
        48: 'Mistig',
        51: 'Lichte motregen',
        53: 'Motregen',
        55: 'Motregen',
        56: 'Ijzel',
        57: 'Ijzel',
        61: 'Lichte regen',
        63: 'Regen',
        65: 'Zware regen',
        66: 'Ijzel',
        67: 'Ijzel',
        71: 'Lichte sneeuw',
        73: 'Sneeuw',
        75: 'Zware sneeuw',
        77: 'Hagel',
        80: 'Lichte buien',
        81: 'Buien',
        82: 'Zware buien',
        85: 'Sneeuwbuien',
        86: 'Sneeuwbuien',
        95: 'Onweer',
        96: 'Onweer met hagel',
        99: 'Zwaar onweer'
    }
    
    return weather_code_mapping.get(weather_code, 'Bewolkt')


def load_data_and_train_models():
    """Load data and train both Decision Tree and Random Forest models"""
    global dt_models, rf_models, dt_r2_scores, rf_r2_scores, available_features, weather_mapping
    
    # API URLs
    trashUrl = "http://host.docker.internal:8080/api/TrashItems/dummy"
    weerUrl = "http://host.docker.internal:8080/api/Weather/"
    
    try:
        print("Loading trash data...")
        try:
            print(f"Making request to: {trashUrl}")
            response = requests.get(trashUrl, timeout=10)
            print(f"Response status: {response.status_code}")
            
            if response.status_code == 200:
                # Convert response to DataFrame
                trash_data = response.json()
                trash = pd.DataFrame(trash_data)
                print(f"✅ Loaded {len(trash)} trash records from API")
            else:
                raise Exception(f"API returned status {response.status_code}")
                
        except Exception as e:
            print(f"❌ Error loading trash data: {e}")
            raise e  # Re-raise om te zien wat er echt gebeurt
        
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
            dt = DecisionTreeRegressor(max_depth=2, random_state=42)
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
    """ECHTE confidence calculation gebaseerd op model internals"""
    try:
        if hasattr(model, 'estimators_'):
            # Random Forest: tree consensus (dit is al goed)
            predictions = np.array([tree.predict(input_data)[0] for tree in model.estimators_])
            mean_pred = np.mean(predictions)
            std_pred = np.std(predictions)
            
            if mean_pred == 0:
                return 0.3
            
            relative_std = std_pred / abs(mean_pred) if abs(mean_pred) > 0 else 1.0
            confidence = max(0.3, min(0.9, 1.0 - relative_std))
            
            return round(confidence, 3)
            
        else:
            # Decision Tree: ECHTE confidence gebaseerd op leaf statistics
            try:
                # Vind welke leaf deze input gebruikt
                leaf_id = model.decision_path(input_data).toarray()[0]
                leaf_indices = np.where(leaf_id)[0]
                final_leaf = leaf_indices[-1]  # Laatste node = leaf
                
                # Hoeveel training samples zitten in deze leaf?
                n_samples_in_leaf = model.tree_.n_node_samples[final_leaf]
                total_samples = model.tree_.n_node_samples[0]  # Root heeft alle samples
                
                # Wat is de impurity van deze leaf? (hoe "zuiver" is de voorspelling)
                leaf_impurity = model.tree_.impurity[final_leaf]
                
                # Sample-based confidence: meer samples = meer betrouwbaar
                sample_ratio = n_samples_in_leaf / total_samples
                sample_confidence = min(0.6, sample_ratio * 5.0)  # Max 0.6 van samples
                
                # Purity-based confidence: lagere impurity = meer betrouwbaar  
                purity_confidence = max(0.1, 1.0 - leaf_impurity)
                purity_confidence = min(0.4, purity_confidence)  # Max 0.4 van purity
                
                # Path depth confidence: diepere path = specifieker
                path_depth = len(leaf_indices)
                depth_confidence = min(0.2, path_depth / 10.0)  # Max 0.2 van depth
                
                # Combineer alle confidence factors
                total_confidence = sample_confidence + purity_confidence + depth_confidence
                
                # Zorg dat het binnen bereik blijft
                final_confidence = max(0.2, min(0.9, total_confidence))
                
                return round(final_confidence, 3)
                
            except Exception as e:
                print(f"Error in DT confidence calculation: {e}")
                # Fallback: gebruik feature importance
                try:
                    max_importance = np.max(model.feature_importances_)
                    return round(max(0.3, min(0.7, max_importance * 2.0)), 3)
                except:
                    return 0.4
            
    except Exception as e:
        print(f"Error calculating confidence for {target_name}: {e}")
        return 0.3

def predict_waste(date_str: str, latitude: float, longitude: float):
    """Voorspel afval voor specifieke datum en locatie (weer wordt automatisch opgehaald)"""
    
    # Haal weer data op van OpenMeteo
    weather_data = get_weather_from_openmeteo(date_str, latitude, longitude)
    temperatuur = weather_data['temperatuur']
    weersverwachting = weather_data['weersverwachting']
    
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
    
    # Make predictions voor alle categorieën
    categories = ['Plastic', 'Papier', 'Organisch', 'Glas']
    predictions = {}
    confidence_scores = {}
    model_used_per_category = {}
    
    for category in categories:
        # Gebruik Random Forest als beschikbaar, anders Decision Tree
        if category in rf_models:
            model = rf_models[category]
            model_type = "random_forest"
        elif category in dt_models:
            model = dt_models[category]
            model_type = "decision_tree"
        else:
            predictions[category] = 0
            confidence_scores[category] = 0.0
            model_used_per_category[category] = "none"
            continue
        
        try:
            # Maak voorspelling
            prediction = model.predict(input_values)[0]
            predictions[category] = max(0, round(prediction))
            
            # Bereken confidence
            confidence = calculate_prediction_confidence(model, input_values, category)
            confidence_scores[category] = confidence
            model_used_per_category[category] = model_type
            
        except Exception as e:
            print(f"Error predicting {category}: {e}")
            predictions[category] = 0
            confidence_scores[category] = 0.0
            model_used_per_category[category] = "error"
    
    # Calculate overall confidence
    avg_confidence = np.mean(list(confidence_scores.values())) if confidence_scores else 0
    
    # Overall model gebruikt
    model_types = list(model_used_per_category.values())
    overall_model = "random_forest" if "random_forest" in model_types else "mixed"
    
    # Return predictions + weather data
    return (predictions, confidence_scores, {}, overall_model, avg_confidence, 
            model_used_per_category, weather_data)

def get_best_model_type():
    """Eenvoudige best model bepaling"""
    # Random Forest is meestal beter
    return "random_forest" if rf_models else "decision_tree"

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
# startup_models()

@router.post("/predict", response_model=PredictionResponse)
async def predict_waste_endpoint(request: PredictionRequest):
    """Predict waste amounts - weather data wordt automatisch opgehaald van OpenMeteo"""
    try:
        # Validate date format
        datetime.strptime(request.datum, '%Y-%m-%d')
        
        # Check if models are loaded
        if not dt_models and not rf_models:
            raise HTTPException(status_code=503, detail="Models not loaded yet")
        
        # Make prediction (weather wordt automatisch opgehaald)
        predictions, confidence_scores, all_model_confidence, model_used, avg_confidence, model_used_per_category, weather_data = predict_waste(
            request.datum,
            request.latitude,
            request.longitude
        )
        
        return PredictionResponse(
            datum=request.datum,
            latitude=request.latitude,
            longitude=request.longitude,
            temperatuur=weather_data['temperatuur'],
            weersverwachting=weather_data['weersverwachting'],
            weather_source=weather_data['weather_source'],
            predictions=predictions,
            confidence_scores=confidence_scores,
            model_used_per_category=model_used_per_category,
            data_source="TrashItems API + Weather API"
        )
        
    except ValueError as e:
        raise HTTPException(status_code=400, detail=f"Invalid date format. Use YYYY-MM-DD: {str(e)}")
    except Exception as e:
        print(f"Prediction error: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Prediction failed: {str(e)}")