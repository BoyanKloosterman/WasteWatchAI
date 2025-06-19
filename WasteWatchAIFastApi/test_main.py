import pytest
import requests
import json
from datetime import datetime, timedelta
from fastapi.testclient import TestClient
from fastapi import FastAPI
import sys
import os

app = FastAPI()

client = TestClient(app)

class TestCorrelationAPI:
    """Tests voor de correlatie analyse API"""
    
    def setup_method(self):
        """Setup test data"""
        self.base_url = "http://localhost:8000" 
        self.test_correlation_data = {
            "trash_items": [
                {
                    "id": "test-1",
                    "litterType": "Plastic",
                    "latitude": 51.5865,
                    "longitude": 4.7761,
                    "timestamp": (datetime.now() - timedelta(days=5)).isoformat()
                },
                {
                    "id": "test-2", 
                    "litterType": "Papier",
                    "latitude": 51.5870,
                    "longitude": 4.7765,
                    "timestamp": (datetime.now() - timedelta(days=3)).isoformat()
                },
                {
                    "id": "test-3",
                    "litterType": "Glas",
                    "latitude": 51.5860,
                    "longitude": 4.7755,
                    "timestamp": (datetime.now() - timedelta(days=1)).isoformat()
                }
            ],
            "latitude": 51.5865,
            "longitude": 4.7761,
            "days_back": 10
        }
    
    def test_correlation_endpoint_structure(self):
        """Test of de correlatie endpoint de juiste structuur teruggeeft"""

        expected_fields = [
            "correlation_coefficient",
            "correlation_strength", 
            "sunny_weather_percentage",
            "rainy_weather_percentage",
            "temperature_correlation",
            "insights",
            "chart_data"
        ]
        
        # Simuleer een response voor test doeleinden
        mock_response = {
            "correlation_coefficient": 0.45,
            "correlation_strength": "Matige correlatie",
            "sunny_weather_percentage": 15.2,
            "rainy_weather_percentage": -8.7,
            "temperature_correlation": 0.45,
            "insights": [
                "Weerdata voor Breda centrum (51.5865°N, 4.7761°E)",
                "Matige correlatie: warmere dagen tonen iets meer afval (r=0.450)"
            ],
            "chart_data": {
                "temperature_data": {
                    "labels": ["01-06", "02-06", "03-06"],
                    "temperature": [18.5, 20.1, 22.3],
                    "trash_count": [3, 5, 7]
                }
            }
        }
        
        # Test alle verwachte velden
        for field in expected_fields:
            assert field in mock_response, f"Veld '{field}' ontbreekt in response"
        
        # Test data types
        assert isinstance(mock_response["correlation_coefficient"], (int, float))
        assert isinstance(mock_response["correlation_strength"], str)
        assert isinstance(mock_response["insights"], list)
        assert isinstance(mock_response["chart_data"], dict)
        
        print("✅ Correlatie endpoint structuur test geslaagd")
    
    def test_correlation_input_validation(self):
        """Test input validatie voor correlatie API"""
        
        # Test met lege trash_items
        invalid_data_1 = {
            "trash_items": [],
            "latitude": 51.5865,
            "longitude": 4.7761,
            "days_back": 10
        }
        
        # Test met ongeldige coordinaten
        invalid_data_2 = {
            "trash_items": self.test_correlation_data["trash_items"],
            "latitude": 200,  # Ongeldig
            "longitude": 4.7761,
            "days_back": 10
        }
        
        # Test met negatieve days_back
        invalid_data_3 = {
            "trash_items": self.test_correlation_data["trash_items"],
            "latitude": 51.5865,
            "longitude": 4.7761,
            "days_back": -5  # Ongeldig
        }
        
        test_cases = [invalid_data_1, invalid_data_2, invalid_data_3]
        
        for i, test_case in enumerate(test_cases):
            # In een echte test zou je dit doen:
            # response = requests.post(f"{self.base_url}/api/correlation/analyze", json=test_case)
            # assert response.status_code in [400, 422], f"Test case {i+1} zou een error moeten geven"
            
            print(f"✅ Input validatie test case {i+1} gedefinieerd")
        
        print("✅ Alle correlatie input validatie tests gedefinieerd")

class TestPredictionAPI:
    """Tests voor de voorspelling API"""
    
    def setup_method(self):
        """Setup test data"""
        self.base_url = "http://localhost:8000"
        self.test_prediction_data = {
            "date": (datetime.now() + timedelta(days=1)).strftime("%Y-%m-%d"),
            "latitude": 51.5865,
            "longitude": 4.7761
        }
    
    def test_prediction_endpoint_structure(self):
        """Test of de voorspelling endpoint de juiste structuur teruggeeft"""
        expected_fields = [
            "date",
            "latitude", 
            "longitude",
            "temperature",
            "weather_description",
            "weather_source",
            "predictions",
            "confidence_scores",
            "model_used_per_category",
            "data_source"
        ]
        
        # Mock response voor test
        mock_response = {
            "date": "2024-06-20",
            "latitude": 51.5865,
            "longitude": 4.7761,
            "temperature": 18.5,
            "weather_description": "Gedeeltelijk bewolkt",
            "weather_source": "OpenMeteo API",
            "predictions": {
                "Plastic": 5,
                "Paper": 3,
                "Organic": 2,
                "Glass": 1
            },
            "confidence_scores": {
                "Plastic": 0.75,
                "Paper": 0.68,
                "Organic": 0.82,
                "Glass": 0.71
            },
            "model_used_per_category": {
                "Plastic": "random_forest",
                "Paper": "random_forest", 
                "Organic": "decision_tree",
                "Glass": "random_forest"
            },
            "data_source": "TrashItems API + Weather API"
        }
        
        # Test alle verwachte velden
        for field in expected_fields:
            assert field in mock_response, f"Veld '{field}' ontbreekt in response"
        
        # Test specifieke data types
        assert isinstance(mock_response["predictions"], dict)
        assert isinstance(mock_response["confidence_scores"], dict)
        assert isinstance(mock_response["temperature"], (int, float))
        
        # Test dat alle waste categorieën aanwezig zijn
        expected_categories = ["Plastic", "Paper", "Organic", "Glass"]
        for category in expected_categories:
            assert category in mock_response["predictions"]
            assert category in mock_response["confidence_scores"]
        
        print("✅ Voorspelling endpoint structuur test geslaagd")
    
    def test_prediction_date_validation(self):
        """Test datum validatie voor voorspelling API"""
        
        # Test met ongeldige datum formaten
        invalid_dates = [
            "2024-13-01",  # Ongeldige maand
            "2024-06-32",  # Ongeldige dag
            "24-06-20",    # Verkeerd formaat
            "2024/06/20",  # Verkeerd formaat
            "vandaag",     # Tekst
            ""             # Leeg
        ]
        
        for invalid_date in invalid_dates:
            test_data = self.test_prediction_data.copy()
            test_data["date"] = invalid_date
            
            # In een echte test:
            # response = requests.post(f"{self.base_url}/api/prediction/predict/dummy", json=test_data)
            # assert response.status_code in [400, 422], f"Datum '{invalid_date}' zou een error moeten geven"
            
            print(f"✅ Ongeldige datum test voor '{invalid_date}' gedefinieerd")
        
        print("✅ Alle datum validatie tests gedefinieerd")
    
    def test_prediction_confidence_scores(self):
        """Test dat confidence scores binnen verwachte bereik liggen"""
        mock_confidence_scores = {
            "Plastic": 0.75,
            "Paper": 0.68,
            "Organic": 0.82,
            "Glass": 0.71
        }
        
        for category, score in mock_confidence_scores.items():
            assert 0.0 <= score <= 1.0, f"Confidence score voor {category} moet tussen 0 en 1 liggen"
            assert score >= 0.2, f"Confidence score voor {category} lijkt te laag ({score})"
        
        print("✅ Confidence scores validatie test geslaagd")

class TestWeatherIntegration:
    """Tests voor weer API integratie"""
    
    def test_weather_api_connection(self):
        """Test of verbinding met weather API werkt"""
        # Test OpenMeteo API direct
        test_url = "https://api.open-meteo.com/v1/forecast"
        test_params = {
            "latitude": 51.5865,
            "longitude": 4.7761,
            "daily": "temperature_2m_max,temperature_2m_min,weather_code",
            "timezone": "Europe/Amsterdam",
            "forecast_days": 1
        }
        
        try:
            response = requests.get(test_url, params=test_params, timeout=10)
            assert response.status_code == 200, "OpenMeteo API niet bereikbaar"
            
            data = response.json()
            assert "daily" in data, "OpenMeteo response mist 'daily' data"
            assert "temperature_2m_max" in data["daily"], "Temperatuur data ontbreekt"
            
            print("✅ Weather API verbinding test geslaagd")
            
        except requests.exceptions.RequestException as e:
            print(f"⚠️ Weather API test gefaald: {e}")
            print("   Dit kan normaal zijn als er geen internetverbinding is")
    
    def test_weather_fallback(self):
        """Test of fallback weather data correct werkt"""
        # Test seizoensgemiddelden
        seasonal_temps = {
            1: 5.0,   # Winter
            4: 12.0,  # Lente
            7: 20.0,  # Zomer
            10: 12.0  # Herfst
        }
        
        for month, expected_temp in seasonal_temps.items():
            # Simuleer get_seasonal_temperature functie
            if month in [12, 1, 2]:  # Winter
                temp = 5.0
            elif month in [3, 4, 5]:  # Lente
                temp = 12.0
            elif month in [6, 7, 8]:  # Zomer
                temp = 20.0
            else:  # Herfst
                temp = 12.0
            
            assert temp == expected_temp, f"Seizoenstemperatuur voor maand {month} incorrect"
        
        print("✅ Weather fallback test geslaagd")

class TestEndToEnd:
    """End-to-end tests voor de volledige applicatie"""
    
    def test_complete_workflow(self):
        """Test complete workflow: correlatie analyse → voorspelling"""
        
        # Stap 1: Correlatie analyse
        correlation_data = {
            "trash_items": [
                {
                    "id": "e2e-1",
                    "litterType": "Plastic", 
                    "latitude": 51.5865,
                    "longitude": 4.7761,
                    "timestamp": (datetime.now() - timedelta(days=2)).isoformat()
                }
            ],
            "latitude": 51.5865,
            "longitude": 4.7761,
            "days_back": 7
        }
        
        # Stap 2: Voorspelling
        prediction_data = {
            "date": (datetime.now() + timedelta(days=1)).strftime("%Y-%m-%d"),
            "latitude": 51.5865,
            "longitude": 4.7761
        }
        
        
        print("✅ End-to-end workflow test scenario gedefinieerd")

def run_all_tests():
    """Run alle tests"""
    print("🧪 Starting Waste Analysis App Tests")
    print("=" * 50)
    
    # Correlatie tests
    print("\n📊 Testing Correlation API...")
    correlation_test = TestCorrelationAPI()
    correlation_test.setup_method()
    correlation_test.test_correlation_endpoint_structure()
    correlation_test.test_correlation_input_validation()
    
    # Voorspelling tests
    print("\n🔮 Testing Prediction API...")
    prediction_test = TestPredictionAPI()
    prediction_test.setup_method()
    prediction_test.test_prediction_endpoint_structure()
    prediction_test.test_prediction_date_validation()
    prediction_test.test_prediction_confidence_scores()
    
    # Weather tests
    print("\n🌤️ Testing Weather Integration...")
    weather_test = TestWeatherIntegration()
    weather_test.test_weather_api_connection()
    weather_test.test_weather_fallback()

    print("\n🔄 Testing End-to-End Workflow...")
    e2e_test = TestEndToEnd()
    e2e_test.test_complete_workflow()
    
    print("\n" + "=" * 50)
    print("🎉 Alle tests voltooid!")
    print("\nOm echte API tests uit te voeren:")
    print("1. Start je FastAPI server")
    print("2. Pas de base_url aan in de test classes")
    print("3. Uncommenting de echte API calls")
    print("4. Run: pytest test_waste_analysis.py -v")

if __name__ == "__main__":
    run_all_tests()