const express = require('express');
const cors = require('./middleware/cors');
const TrashDataGenerator = require('./utils/dataGenerator');

const app = express();
const PORT = process.env.PORT || 3001;

// Initialize data generator
const dataGenerator = new TrashDataGenerator();

// Middleware
app.use(cors);
app.use(express.json());

// Health check endpoint
app.get('/health', (req, res) => {
    res.json({ status: 'OK', message: 'Mock API is running and posting to real database' });
});

// Manual generate endpoint
app.post('/generate', async (req, res) => {
    try {
        const { count = 5 } = req.body;
        console.log(`🚀 Manual generation of ${count} items requested...`);
        const results = await dataGenerator.addNewItems(parseInt(count));
        res.json({
            message: `Generated ${results.length} new items and posted to database`,
            items: results
        });
    } catch (error) {
        console.error('Error generating items:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Random interval generator functie
function scheduleRandomGeneration() {
    // Random interval tussen 20 seconden en 3 minuten
    const minInterval = 20000;  // 20 seconden
    const maxInterval = 180000; // 3 minuten
    const randomInterval = Math.floor(Math.random() * (maxInterval - minInterval + 1)) + minInterval;
    
    // Random aantal items (meestal 1-2, soms meer)
    const randomCount = Math.random() < 0.8 ? 
        Math.floor(Math.random() * 2) + 1 :  // 80% kans op 1-2 items
        Math.floor(Math.random() * 4) + 3;   // 20% kans op 3-6 items
    
    setTimeout(async () => {
        try {
            const intervalMinutes = (randomInterval / 60000).toFixed(1);
            console.log(`🎯 Random detection! (after ${intervalMinutes} min) - Processing ${randomCount} items...`);
            
            const newItems = await dataGenerator.addNewItems(randomCount);
            console.log(`✅ Posted ${newItems.length} new detections to database`);
            
            // Plan de volgende random generatie
            scheduleRandomGeneration();
        } catch (error) {
            console.error('❌ Error in random detection simulation:', error);
            // Plan opnieuw ook bij error
            scheduleRandomGeneration();
        }
    }, randomInterval);
    
    console.log(`⏱️  Next detection scheduled in ${(randomInterval / 1000).toFixed(0)} seconds (${randomCount} items)`);
}

// Start direct met random generatie
console.log('🎲 Starting live trash detection simulation...');
scheduleRandomGeneration();

// Start the server
app.listen(PORT, () => {
    console.log(`🚀 Live Trash Detection Simulator running on http://localhost:${PORT}`);
    console.log(`📊 Posting live data to: http://localhost:8080/api/TrashItems/dummy`);
    console.log(`🎯 Random detections: every 20s-3min, 1-6 items per detection`);
    console.log(`🧪 Manual generation: POST http://localhost:${PORT}/generate`);
});