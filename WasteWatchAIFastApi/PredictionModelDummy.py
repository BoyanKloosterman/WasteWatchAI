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
import time

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
    date: str 
    latitude: float = 51.5890  # Default voor Breda centrum
    longitude: float = 4.7750  # Default voor Breda centrum

class PredictionResponse(BaseModel):
    date: str 
    latitude: float
    longitude: float
    temperature: float 
    weather_description: str 
    weather_source: str
    predictions: Dict[str, int]
    confidence_scores: Dict[str, float]
    model_used_per_category: Dict[str, str] = {}
    data_source: str


def get_weather_from_openmeteo(date_str: str, latitude: float, longitude: float):
    """Get weather data from OpenMeteo API for specific date and location"""
    try:
        # Parse date
        target_date = datetime.strptime(date_str, '%Y-%m-%d').date()
        today = datetime.now().date()
        
        # Determine if historical data or forecast
        if target_date <= today:
            # Historical data
            api_url = "https://archive-api.open-meteo.com/v1/archive"
            temp_params = 'temperature_2m_mean,temperature_2m_max,temperature_2m_min'
        else:
            # Forecast data - use more parameters
            api_url = "https://api.open-meteo.com/v1/forecast"
            temp_params = 'temperature_2m_max,temperature_2m_min,temperature_2m_mean'
        
        # API parameters - request multiple temperature types
        params = {
            'latitude': latitude,
            'longitude': longitude,
            'start_date': date_str,
            'end_date': date_str,
            'daily': f'{temp_params},weather_code',
            'timezone': 'Europe/Amsterdam'
        }
        
        print(f"🌤️ Fetching weather for {date_str} from OpenMeteo...")
        response = requests.get(api_url, params=params, timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            print(f"🔍 OpenMeteo response: {data}")
            
            if 'daily' in data and data['daily']['time']:
                daily_data = data['daily']
                
                # Try different temperature sources
                temperature = None
                temp_source = None
                
                # 1. Try temperature_2m_mean
                if 'temperature_2m_mean' in daily_data and daily_data['temperature_2m_mean']:
                    temp_val = daily_data['temperature_2m_mean'][0]
                    if temp_val is not None:
                        temperature = temp_val
                        temp_source = "mean"
                
                # 2. If mean doesn't work, calculate from max/min
                if temperature is None:
                    temp_max = None
                    temp_min = None
                    
                    if 'temperature_2m_max' in daily_data and daily_data['temperature_2m_max']:
                        temp_max = daily_data['temperature_2m_max'][0]
                    
                    if 'temperature_2m_min' in daily_data and daily_data['temperature_2m_min']:
                        temp_min = daily_data['temperature_2m_min'][0]
                    
                    if temp_max is not None and temp_min is not None:
                        temperature = (temp_max + temp_min) / 2
                        temp_source = f"calculated from max({temp_max}°C) and min({temp_min}°C)"
                    elif temp_max is not None:
                        temperature = temp_max - 3  # Estimate: max - 3°C for average
                        temp_source = f"estimated from max({temp_max}°C)"
                    elif temp_min is not None:
                        temperature = temp_min + 3  # Estimate: min + 3°C for average
                        temp_source = f"estimated from min({temp_min}°C)"
                
                # 3. Get weather code
                weather_code = daily_data.get('weather_code', [None])[0] if daily_data.get('weather_code') else None
                
                # 4. If we have temperature, use it
                if temperature is not None and isinstance(temperature, (int, float)) and -50 <= temperature <= 60:
                    weather_description = map_weather_code_to_description(weather_code) if weather_code is not None else 'Cloudy'
                    
                    print(f"✅ Weather data: {temperature}°C ({temp_source}), {weather_description}")
                    
                    return {
                        'temperature': round(float(temperature), 1),
                        'weather_description': weather_description,
                        'weather_source': f'OpenMeteo API ({temp_source})'
                    }
                
                # 5. If only weather code available
                elif weather_code is not None:
                    # Use seasonal average + weather code
                    month = datetime.strptime(date_str, '%Y-%m-%d').month
                    seasonal_temp = get_seasonal_temperature(month)
                    weather_description = map_weather_code_to_description(weather_code)
                    
                    print(f"✅ Partial weather data: {seasonal_temp}°C (seasonal), {weather_description} (OpenMeteo)")
                    
                    return {
                        'temperature': seasonal_temp,
                        'weather_description': weather_description,
                        'weather_source': 'OpenMeteo weather + seasonal temperature'
                    }
                
                else:
                    raise Exception(f"No valid temperature or weather data: temp={temperature}, code={weather_code}")
            else:
                raise Exception(f"Invalid OpenMeteo response structure: {data}")
        else:
            raise Exception(f"OpenMeteo API returned status {response.status_code}: {response.text}")
            
    except Exception as e:
        print(f"❌ OpenMeteo API failed: {e}")
        
        # Complete fallback
        month = datetime.strptime(date_str, '%Y-%m-%d').month
        fallback_temp = get_seasonal_temperature(month)
        fallback_weather = get_seasonal_weather(month)
        
        print(f"🔄 Using complete fallback: {fallback_temp}°C, {fallback_weather}")
        
        return {
            'temperature': fallback_temp,
            'weather_description': fallback_weather,
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
        56: 'IJzel',
        57: 'IJzel',
        61: 'Lichte regen',
        63: 'Regen',
        65: 'Zware regen',
        66: 'IJzel',
        67: 'IJzel',
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

def get_seasonal_weather(month):
    """Get typical weather for season in Netherlands (Dutch descriptions)"""
    if month in [12, 1, 2]:  # Winter
        return 'Bewolkt'
    elif month in [3, 4, 5]:  # Spring
        return 'Gedeeltelijk bewolkt'
    elif month in [6, 7, 8]:  # Summer
        return 'Zonnig'
    else:  # Autumn
        return 'Regenachtig'

def get_seasonal_temperature(month):
    """Get seasonal average temperature for Netherlands"""
    if month in [12, 1, 2]:  # Winter
        return 5.0
    elif month in [3, 4, 5]:  # Spring
        return 12.0
    elif month in [6, 7, 8]:  # Summer
        return 20.0
    else:  # Autumn (9, 10, 11)
        return 12.0

def load_data_and_train_models():
    """Load dummy data and train both Decision Tree and Random Forest models with retry logic"""
    global dt_models, rf_models, dt_r2_scores, rf_r2_scores, available_features, weather_mapping
    
    # Container startup retry configuration
    max_retries = 60  # Increased for container startup
    retry_delay = 10  # Longer delay for container startup
    base_url = "http://host.docker.internal:8080"  # Since this URL works
    
    working_base_url = None
    trash_data = None
    weather_data = None
    
    print(f"⏳ Waiting for API container to start at {base_url} (DUMMY)")
    print(f"🔄 Will retry {max_retries} times with {retry_delay}s intervals (max {max_retries * retry_delay / 60:.1f} minutes)")
    
    # Keep trying until the API container is ready
    for attempt in range(1, max_retries + 1):
        try:
            print(f"🎲 Dummy API check {attempt}/{max_retries} ({attempt * retry_delay}s elapsed)")
            
            # Test if the API container is responding
            trash_url = f"{base_url}/api/TrashItems/dummy"  # Use dummy endpoint
            
            print(f"   📡 Testing dummy API: {trash_url}")
            
            response = requests.get(trash_url, timeout=15)
            
            if response.status_code == 200:
                trash_data = response.json()
                working_base_url = base_url
                print(f"🎉 SUCCESS! Dummy API container is ready!")
                print(f"✅ Loaded {len(trash_data)} dummy trash records")
                break
            else:
                print(f"   ⚠️  Dummy API returned status {response.status_code} (container might be starting)")
                
        except requests.exceptions.ConnectionError as e:
            print(f"   ⏳ Connection refused - API container not ready yet")
        except requests.exceptions.Timeout as e:
            print(f"   ⏳ Request timeout - API container might be starting")
        except Exception as e:
            print(f"   ❌ Unexpected error: {e}")
        
        # Wait before retry
        if attempt < max_retries:
            print(f"   💤 Waiting {retry_delay}s for API container to start...")
            time.sleep(retry_delay)
        else:
            print("❌ Max wait time exceeded. API container didn't start in time.")
            return False  # No fallback for dummy - just fail
    
    # If we got trash data, try to get weather data
    if working_base_url and trash_data:
        try:
            weather_url = f"{working_base_url}/api/Weather/"
            print(f"🌤️ Getting dummy weather data: {weather_url}")
            
            weather_response = requests.get(weather_url, timeout=15)
            
            if weather_response.status_code == 200:
                weather_data = weather_response.json()
                print(f"✅ Dummy weather data loaded! {len(weather_data)} records")
            else:
                print(f"⚠️  Weather API returned status {weather_response.status_code}")
                print("⚠️  Continuing with dummy trash data only")
                
        except Exception as e:
            print(f"⚠️  Weather API error: {e}")
            print("⚠️  Continuing with dummy trash data only")
    
    # Process the data (only real API data, no fallback)
    try:
        if not trash_data:
            print("❌ No dummy data available - API container not responding")
            return False
        
        print("📊 Processing REAL dummy API data...")
        trash = pd.DataFrame(trash_data)
        
        # Process weather data
        if weather_data:
            print("🌤️ Processing REAL weather data...")
            weather_df = pd.DataFrame(weather_data)
        else:
            print("🌤️ No weather data available")
            weather_df = pd.DataFrame()  # Empty DataFrame
        
        # Continue with existing data processing and model training...
        print("🚀 Starting dummy model training...")
        
        # Convert timestamp to datetime - handle various ISO8601 formats
        trash['timestamp'] = pd.to_datetime(trash['timestamp'], format='ISO8601')
        trash['datum'] = trash['timestamp'].dt.date
        trash['hour'] = trash['timestamp'].dt.hour
        trash['min'] = trash['timestamp'].dt.minute
        trash['year'] = trash['timestamp'].dt.year
        trash['month'] = trash['timestamp'].dt.month
        trash['day'] = trash['timestamp'].dt.day
        trash['weekday'] = trash['timestamp'].dt.weekday
        
        # Group by date and category
        daily_summary = trash.groupby(['datum', 'litterType']).size().reset_index(name='count')
        daily_data = daily_summary.pivot(index='datum', columns='litterType', values='count').fillna(0)
        daily_data = daily_data.reset_index()
        
        # Add location data (use mean coordinates)
        daily_data['latitude'] = trash.groupby('datum')['latitude'].mean().values
        daily_data['longitude'] = trash.groupby('datum')['longitude'].mean().values
        
        # Add date features
        daily_data['year'] = pd.to_datetime(daily_data['datum']).dt.year
        daily_data['month'] = pd.to_datetime(daily_data['datum']).dt.month
        daily_data['day'] = pd.to_datetime(daily_data['datum']).dt.day
        daily_data['weekday'] = pd.to_datetime(daily_data['datum']).dt.weekday
        
        # Merge weather data if available
        if len(weather_df) > 0:
            print("🔗 Merging weather data...")
            # Prepare weather data
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
            daily_data = pd.merge(daily_data, weather_df[['datum', 'temperatuur', 'weersverwachting']], 
                               on='datum', how='left')
            print("✅ Weather data merged successfully")
        
        # Fill missing weather data with defaults (no fallback generation)
        daily_data['temperatuur'] = daily_data['temperatuur'].fillna(15.0)
        daily_data['weersverwachting'] = daily_data['weersverwachting'].fillna('Bewolkt')
        
        # Add calculated features
        daily_data['is_weekend'] = (daily_data['weekday'] >= 5).astype(int)
        daily_data['seizoen'] = daily_data['month'].map({
            12: 0, 1: 0, 2: 0,
            3: 1, 4: 1, 5: 1,
            6: 2, 7: 2, 8: 2,
            9: 3, 10: 3, 11: 3
        })
        
        # Weather mapping
        weather_mapping = {
            'Zonnig': 0, 'Gedeeltelijk bewolkt': 1, 'Bewolkt': 2,
            'Regenachtig': 3, 'Onweer': 4, 'Sneeuw': 5, 'Mistig': 6, 'Onbekend': 1
        }
        
        daily_data['weer_numeriek'] = daily_data['weersverwachting'].map(weather_mapping).fillna(1)
        
        # Define available features
        available_features = [
            'latitude', 'longitude', 'year', 'month', 'day', 'weekday',
            'temperatuur', 'weer_numeriek', 'is_weekend', 'seizoen'
        ]
        
        # Target categories
        targets = ['Plastic', 'Papier', 'Organisch', 'Glas']
        
        print(f"🎯 Training dummy models for {len(targets)} waste categories...")
        print(f"📊 Using {len(daily_data)} days of dummy data")
        
        # Train models for each category
        for target_name in targets:
            if target_name not in daily_data.columns:
                print(f"⚠️  Target {target_name} not found in dummy data, skipping")
                continue
            
            target = daily_data[target_name]
            
            # Skip if target has no variance
            if target.std() == 0:
                print(f"⚠️  Target {target_name} has no variance, skipping")
                continue
            
            # Train-test split
            X_train, X_test, y_train, y_test = train_test_split(
                daily_data[available_features].values,
                target.values,
                test_size=0.3, random_state=42
            )
            
            # Train Decision Tree
            dt = DecisionTreeRegressor(max_depth=5, random_state=42)
            dt.fit(X_train, y_train)
            dt_pred = dt.predict(X_test)
            dt_r2 = r2_score(y_test, dt_pred)
            
            # Train Random Forest
            rf = RandomForestRegressor(n_estimators=100, max_depth=8, random_state=42)
            rf.fit(X_train, y_train)
            rf_pred = rf.predict(X_test)
            rf_r2 = r2_score(y_test, rf_pred)
            
            # Store models and scores
            dt_models[target_name] = dt
            rf_models[target_name] = rf
            dt_r2_scores[target_name] = max(0, dt_r2)
            rf_r2_scores[target_name] = max(0, rf_r2)
            
            print(f"  ✅ {target_name}: DT R²={dt_r2:.3f}, RF R²={rf_r2:.3f}")
        
        print(f"🎉 Dummy model training completed! Trained {len(dt_models)} models")
        return True
        
    except Exception as e:
        print(f"❌ Error in dummy model training: {e}")
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
    """Predict waste for specific date and location (weather is automatically fetched)"""
    
    # Get weather data from OpenMeteo
    weather_data = get_weather_from_openmeteo(date_str, latitude, longitude)
    temperature = weather_data['temperature']
    weather_description = weather_data['weather_description']
    
    # Convert date string to datetime
    date_obj = datetime.strptime(date_str, '%Y-%m-%d').date()
    
    # Extract date features
    year = date_obj.year
    month = date_obj.month
    day = date_obj.day
    weekday = date_obj.weekday()
    
    # Calculate derived features
    is_weekend = 1 if weekday >= 5 else 0
    season_mapping = {
        12: 0, 1: 0, 2: 0,  # Winter
        3: 1, 4: 1, 5: 1,   # Spring  
        6: 2, 7: 2, 8: 2,   # Summer
        9: 3, 10: 3, 11: 3  # Autumn
    }
    season = season_mapping[month]
    
    # Map weather description to numeric (update mapping for English)
    weather_mapping_english = {
        'Clear sky': 0,
        'Mainly clear': 1,
        'Partly cloudy': 1,
        'Overcast': 2,
        'Cloudy': 2,
        'Fog': 6,
        'Light drizzle': 3,
        'Moderate drizzle': 3,
        'Dense drizzle': 3,
        'Slight rain': 3,
        'Moderate rain': 3,
        'Heavy rain': 3,
        'Rainy': 3,
        'Thunderstorm': 4,
        'Slight snow fall': 5,
        'Moderate snow fall': 5,
        'Heavy snow fall': 5,
        'Unknown': 1
    }
    
    weather_num = weather_mapping_english.get(weather_description, 1)
    
    # Create input data
    input_values = np.array([[
        latitude, longitude, year, month, day, weekday,
        temperature, weather_num, is_weekend, season
    ]])
    
    # Make predictions for all categories
    categories = ['Plastic', 'Paper', 'Organic', 'Glass']  # English names
    predictions = {}
    confidence_scores = {}
    model_used_per_category = {}
    
    # Map to Dutch model names (if your models were trained with Dutch names)
    category_mapping = {
        'Plastic': 'Plastic',
        'Paper': 'Papier', 
        'Organic': 'Organisch',
        'Glass': 'Glas'
    }
    
    for category in categories:
        dutch_category = category_mapping[category]
        
        # Use Random Forest if available, otherwise Decision Tree
        if dutch_category in rf_models:
            model = rf_models[dutch_category]
            model_type = "random_forest"
        elif dutch_category in dt_models:
            model = dt_models[dutch_category]
            model_type = "decision_tree"
        else:
            predictions[category] = 0
            confidence_scores[category] = 0.0
            model_used_per_category[category] = "none"
            continue
        
        try:
            # Make prediction
            prediction = model.predict(input_values)[0]
            predictions[category] = max(0, round(prediction))
            
            # Calculate confidence
            confidence = calculate_prediction_confidence(model, input_values, dutch_category)
            confidence_scores[category] = confidence
            model_used_per_category[category] = model_type
            
        except Exception as e:
            print(f"Error predicting {category}: {e}")
            predictions[category] = 0
            confidence_scores[category] = 0.0
            model_used_per_category[category] = "error"
    
    # Calculate overall confidence
    avg_confidence = np.mean(list(confidence_scores.values())) if confidence_scores else 0
    
    # Overall model used
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
def startup_models_dummy():
    """Initialize models on startup"""
    print("Starting Waste Prediction API...")
    success = load_data_and_train_models()
    if not success:
        print("Warning: Failed to load data and train models")
    else:
        print("Models loaded successfully!")

# Roep startup functie aan
# startup_models()

@router.post("/predict/dummy", response_model=PredictionResponse)
async def predict_waste_endpoint(request: PredictionRequest):
    """Predict waste amounts - weather data is automatically fetched from OpenMeteo"""
    try:
        # Validate date format
        datetime.strptime(request.date, '%Y-%m-%d')
        
        # Check if models are loaded
        if not dt_models and not rf_models:
            raise HTTPException(status_code=503, detail="Models not loaded yet")
        
        # Make prediction (weather is automatically fetched)
        predictions, confidence_scores, all_model_confidence, model_used, avg_confidence, model_used_per_category, weather_data = predict_waste(
            request.date,
            request.latitude,
            request.longitude
        )
        
        return PredictionResponse(
            date=request.date,
            latitude=request.latitude,
            longitude=request.longitude,
            temperature=weather_data['temperature'],
            weather_description=weather_data['weather_description'],
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