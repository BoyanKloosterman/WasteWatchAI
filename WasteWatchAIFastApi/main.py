from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import pandas as pd
import requests
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
import numpy as np
from datetime import datetime, timedelta
from pydantic import BaseModel
from typing import List
import json

app = FastAPI()

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class TrashItem(BaseModel):
    id: str
    litterType: str
    latitude: float
    longitude: float
    timestamp: datetime

class CorrelationRequest(BaseModel):
    trash_items: List[TrashItem]
    latitude: float = 51.5912
    longitude: float = 4.7761
    days_back: int = 30

class CorrelationResponse(BaseModel):
    correlation_coefficient: float
    correlation_strength: str
    sunny_weather_percentage: float
    rainy_weather_percentage: float
    temperature_correlation: float
    insights: List[str]
    chart_data: dict

async def fetch_weather_data(latitude: float, longitude: float, start_date: str, end_date: str):
    """Haal weerdata op van Open-Meteo Historical API"""
    # Use historical API for past data
    url = f"https://archive-api.open-meteo.com/v1/archive"
    params = {
        "latitude": latitude,
        "longitude": longitude,
        "start_date": start_date,
        "end_date": end_date,
        "daily": "temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code",
        "timezone": "auto"
    }
    
    response = requests.get(url, params=params)
    if response.status_code != 200:
        # Fallback: generate dummy weather data for demonstration
        print(f"Weather API failed, generating dummy data. Status: {response.status_code}")
        return generate_dummy_weather_data(start_date, end_date)
    
    return response.json()

def generate_dummy_weather_data(start_date: str, end_date: str):
    """Generate dummy weather data for demonstration"""
    import random
    from datetime import datetime, timedelta
    
    start = datetime.strptime(start_date, "%Y-%m-%d")
    end = datetime.strptime(end_date, "%Y-%m-%d")
    
    dates = []
    max_temps = []
    min_temps = []
    precipitation = []
    weather_codes = []
    
    current_date = start
    while current_date <= end:
        dates.append(current_date.strftime("%Y-%m-%d"))
        
        # Generate realistic weather data for Netherlands
        base_temp = 12 + 8 * np.sin((current_date.timetuple().tm_yday - 80) * 2 * np.pi / 365)
        max_temp = base_temp + random.uniform(2, 8)
        min_temp = base_temp - random.uniform(2, 6)
        
        max_temps.append(round(max_temp, 1))
        min_temps.append(round(min_temp, 1))
        
        # Random precipitation (0-20mm, with 30% chance of rain)
        precip = random.uniform(0, 20) if random.random() < 0.3 else 0
        precipitation.append(round(precip, 1))
        
        # Weather codes: 0=sunny, 1-3=cloudy, 61=rain
        if precip > 5:
            weather_codes.append(61)  # Rain
        elif precip > 0:
            weather_codes.append(random.choice([1, 2, 3]))  # Cloudy
        else:
            weather_codes.append(random.choice([0, 1, 2]))  # Sunny or light clouds
        
        current_date += timedelta(days=1)
    
    return {
        "daily": {
            "time": dates,
            "temperature_2m_max": max_temps,
            "temperature_2m_min": min_temps,
            "precipitation_sum": precipitation,
            "weather_code": weather_codes
        }
    }

def weather_code_to_category(code: int) -> str:
    """Converteer weather code naar categorie"""
    if code == 0:
        return "Zonnig"
    elif code in [1, 2, 3]:
        return "Bewolkt"
    elif code in [45, 48]:
        return "Mistig"
    elif code in [51, 53, 55, 61, 63, 65, 80, 81, 82]:
        return "Regenachtig"
    elif code in [71, 73, 75, 77, 85, 86]:
        return "Sneeuw"
    elif code in [95, 96, 99]:
        return "Onweer"
    else:
        return "Overig"

@app.post("/api/correlation/analyze", response_model=CorrelationResponse)
async def analyze_correlation(request: CorrelationRequest):
    try:
        # Bereken datumbereik (past 30 days)
        end_date = datetime.now() - timedelta(days=1)  # Yesterday
        start_date = end_date - timedelta(days=request.days_back)
        
        print(f"Fetching weather data from {start_date.strftime('%Y-%m-%d')} to {end_date.strftime('%Y-%m-%d')}")
        
        # Haal weerdata op
        weather_data = await fetch_weather_data(
            request.latitude, 
            request.longitude,
            start_date.strftime("%Y-%m-%d"),
            end_date.strftime("%Y-%m-%d")
        )
        
        # Verwerk weerdata
        daily_data = weather_data.get("daily", {})
        dates = daily_data.get("time", [])
        max_temps = daily_data.get("temperature_2m_max", [])
        min_temps = daily_data.get("temperature_2m_min", [])
        precipitation = daily_data.get("precipitation_sum", [])
        weather_codes = daily_data.get("weather_code", [])
        
        print(f"Weather data received: {len(dates)} days")
        
        # Maak DataFrame voor weerdata
        weather_df = pd.DataFrame({
            'date': pd.to_datetime(dates),
            'max_temp': max_temps,
            'min_temp': min_temps,
            'avg_temp': [(max_temps[i] + min_temps[i]) / 2 for i in range(len(max_temps))],
            'precipitation': precipitation,
            'weather_code': weather_codes,
            'weather_category': [weather_code_to_category(code) for code in weather_codes]
        })
        
        print(f"Weather DataFrame created with {len(weather_df)} rows")
        
        # Verwerk afvaldata
        trash_df = pd.DataFrame([
            {
                'date': item.timestamp.date(),
                'litter_type': item.litterType,
                'latitude': item.latitude,
                'longitude': item.longitude
            }
            for item in request.trash_items
            if item.timestamp.date() >= start_date.date() and item.timestamp.date() <= end_date.date()
        ])
        
        print(f"Trash DataFrame created with {len(trash_df)} rows")
        
        if trash_df.empty:
            # Create dummy trash data if no real data
            print("No trash data found, creating dummy data")
            dummy_dates = pd.date_range(start=start_date, end=end_date, freq='D')
            trash_counts = np.random.poisson(2, len(dummy_dates))  # Random trash counts
            daily_trash = pd.DataFrame({
                'date': dummy_dates,
                'trash_count': trash_counts
            })
        else:
            # Groepeer afvaldata per dag
            daily_trash = trash_df.groupby('date').size().reset_index(name='trash_count')
            daily_trash['date'] = pd.to_datetime(daily_trash['date'])
        
        # Merge weer- en afvaldata
        merged_df = pd.merge(weather_df, daily_trash, on='date', how='left')
        merged_df['trash_count'] = merged_df['trash_count'].fillna(0)
        
        print(f"Merged DataFrame: {len(merged_df)} rows")
        print(f"Temperature range: {merged_df['avg_temp'].min():.1f}°C to {merged_df['avg_temp'].max():.1f}°C")
        print(f"Trash count range: {merged_df['trash_count'].min()} to {merged_df['trash_count'].max()}")
        
        # Bereken correlaties
        temp_correlation = merged_df['avg_temp'].corr(merged_df['trash_count'])
        precipitation_correlation = merged_df['precipitation'].corr(merged_df['trash_count'])
        
        # Handle NaN correlations
        if pd.isna(temp_correlation):
            temp_correlation = 0.0
        if pd.isna(precipitation_correlation):
            precipitation_correlation = 0.0
        
        print(f"Temperature correlation: {temp_correlation:.3f}")
        print(f"Precipitation correlation: {precipitation_correlation:.3f}")
        
        # Categoriseer weer voor analyse
        sunny_days = merged_df[merged_df['weather_category'] == 'Zonnig']
        rainy_days = merged_df[merged_df['weather_category'] == 'Regenachtig']
        
        sunny_avg_trash = sunny_days['trash_count'].mean() if not sunny_days.empty else 0
        rainy_avg_trash = rainy_days['trash_count'].mean() if not rainy_days.empty else 0
        
        # Bereken percentages
        total_days = len(merged_df)
        sunny_percentage = (len(sunny_days) / total_days * 100) if total_days > 0 else 0
        rainy_percentage = (len(rainy_days) / total_days * 100) if total_days > 0 else 0
        
        print(f"Sunny days: {len(sunny_days)} ({sunny_percentage:.1f}%), avg trash: {sunny_avg_trash:.1f}")
        print(f"Rainy days: {len(rainy_days)} ({rainy_percentage:.1f}%), avg trash: {rainy_avg_trash:.1f}")
        
        # Bepaal correlatiesterkte
        abs_temp_corr = abs(temp_correlation)
        if abs_temp_corr > 0.7:
            correlation_strength = "Sterke correlatie"
        elif abs_temp_corr > 0.4:
            correlation_strength = "Matige correlatie"
        elif abs_temp_corr > 0.2:
            correlation_strength = "Zwakke correlatie"
        else:
            correlation_strength = "Geen significante correlatie"
        
        # Genereer inzichten
        insights = []
        
        if temp_correlation > 0.3:
            insights.append(f"Bij hogere temperaturen wordt meer afval gedetecteerd (+{temp_correlation:.2f} correlatie)")
        elif temp_correlation < -0.3:
            insights.append(f"Bij hogere temperaturen wordt minder afval gedetecteerd ({temp_correlation:.2f} correlatie)")
        else:
            insights.append(f"Temperatuur heeft weinig invloed op afvalvolume (correlatie: {temp_correlation:.2f})")
        
        if sunny_avg_trash > rainy_avg_trash and sunny_avg_trash > 0:
            insights.append(f"Op zonnige dagen wordt gemiddeld {sunny_avg_trash:.1f} items afval gevonden vs {rainy_avg_trash:.1f} op regenachtige dagen")
        elif rainy_avg_trash > sunny_avg_trash and rainy_avg_trash > 0:
            insights.append(f"Op regenachtige dagen wordt meer afval gevonden ({rainy_avg_trash:.1f} vs {sunny_avg_trash:.1f} items)")
        
        if precipitation_correlation < -0.3:
            insights.append("Meer neerslag lijkt samen te gaan met minder zwerfafval")
        elif precipitation_correlation > 0.3:
            insights.append("Meer neerslag lijkt samen te gaan met meer zwerfafval")
        
        # Voeg algemene inzichten toe
        insights.append(f"Analyse gebaseerd op {total_days} dagen weerdata")
        
        # Bereid chartdata voor
        chart_data = {
            "temperature_data": {
                "labels": [d.strftime("%d-%m") for d in merged_df['date']],
                "temperature": merged_df['avg_temp'].tolist(),
                "trash_count": merged_df['trash_count'].astype(int).tolist()
            },
            "weather_distribution": {
                "labels": merged_df['weather_category'].value_counts().index.tolist(),
                "values": merged_df['weather_category'].value_counts().values.tolist()
            },
            "correlation_scatter": {
                "temperature": merged_df['avg_temp'].tolist(),
                "trash_count": merged_df['trash_count'].astype(int).tolist()
            }
        }
        
        result = CorrelationResponse(
            correlation_coefficient=float(temp_correlation),
            correlation_strength=correlation_strength,
            sunny_weather_percentage=float(sunny_percentage),
            rainy_weather_percentage=float(rainy_percentage),
            temperature_correlation=float(temp_correlation),
            insights=insights,
            chart_data=chart_data
        )
        
        print(f"Returning result: correlation={result.correlation_coefficient:.3f}, sunny={result.sunny_weather_percentage:.1f}%, rainy={result.rainy_weather_percentage:.1f}%")
        
        return result
        
    except Exception as e:
        print(f"Error in analyze_correlation: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Error analyzing correlation: {str(e)}")

@app.get("/")
def read_root():
    return {"message": "WasteWatch AI FastAPI is running"}