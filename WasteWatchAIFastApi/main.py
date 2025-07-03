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
from contextlib import asynccontextmanager
from PredictionModelDummy import router as predictionDummy_router
from PredictionModel import router as prediction_router
from CorrelationModel import router as correlation_router


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    print("🚀 Starting WasteWatch AI FastAPI...")
    
    # Import here to avoid circular imports
    from PredictionModelDummy import startup_models_dummy
    from PredictionModel import startup_models
    # Try to initialize models with fallback
    try:
        print("📊 Initializing prediction models...")
        startup_models_dummy()
        startup_models()
        print("✅ Models initialized successfully!")
    except Exception as e:
        print(f"⚠️  Model initialization failed, but server will continue: {e}")
    
    yield
    
    # Shutdown
    print("🛑 Shutting down WasteWatch AI FastAPI...")

app = FastAPI(lifespan=lifespan)

app.include_router(predictionDummy_router, prefix="/api/prediction", tags=["Prediction"])
app.include_router(prediction_router, prefix="/api/prediction", tags=["Prediction"])
app.include_router(correlation_router, prefix="/api/correlation", tags=["Correlation"])

# CORS middleware - exact zoals in main.py
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
def read_root():
    return {"message": "WasteWatch AI FastAPI is running"}

@app.get("/health")
def health_check():
    return {"status": "ok", "timestamp": datetime.now().isoformat()}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)