const fs = require('fs');
const path = require('path');
const AuthService = require('./authService');

class TrashDataGenerator {
    constructor() {
        this.litterTypes = ['Plastic', 'Papier', 'Organisch', 'Glas'];
        this.currentId = 1;
        // Post naar DummyTrashItems endpoint
       this.realApiUrl = process.env.API_URL || 'http://localhost:8080/api/TrashItems/dummy';
        
        // Initialize auth service
        this.authService = new AuthService();
        
        // Breda coordinate ranges voor realistische locaties
        this.locations = [
            { name: 'Grote Markt', lat: [51.5890, 51.5900], lng: [4.7750, 4.7765] },
            { name: 'Centraal Station', lat: [51.5953, 51.5963], lng: [4.7787, 4.7797] },
            { name: 'Valkenberg Park', lat: [51.5929, 51.5939], lng: [4.7791, 4.7801] },
            { name: 'Havermarkt', lat: [51.5920, 51.5925], lng: [4.7685, 4.7695] },
            { name: 'Wilhelminapark', lat: [51.5860, 51.5866], lng: [4.7848, 4.7856] }
        ];
        
        // Afval type waarschijnlijkheden
        this.litterProbabilities = {
            'Plastic': 0.45,
            'Papier': 0.25,
            'Organisch': 0.20,
            'Glas': 0.10
        };
    }

    getRandomLocation() {
        const location = this.locations[Math.floor(Math.random() * this.locations.length)];
        const lat = location.lat[0] + Math.random() * (location.lat[1] - location.lat[0]);
        const lng = location.lng[0] + Math.random() * (location.lng[1] - location.lng[0]);
        
        return {
            Latitude: parseFloat(lat.toFixed(6)),
            Longitude: parseFloat(lng.toFixed(6))
        };
    }

    getRandomLitterType() {
        const rand = Math.random();
        let cumulative = 0;
        
        for (const [type, probability] of Object.entries(this.litterProbabilities)) {
            cumulative += probability;
            if (rand <= cumulative) {
                return type;
            }
        }
        return 'Plastic';
    }

    getRandomTimestamp() {
        const now = new Date();
        
        const timezoneOffset = 2 * 60 * 60 * 1000; // 2 uur in milliseconden
        const dutchTime = new Date(now.getTime() + timezoneOffset);
        
        const secondsBack = Math.floor(Math.random() * 60);
        const targetDate = new Date(dutchTime.getTime() - (secondsBack * 1000));
        
        return targetDate.toISOString();
    }

    generateDummyTrashItem() {
        const location = this.getRandomLocation();
        return {
            LitterType: this.getRandomLitterType(),
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            Timestamp: this.getRandomTimestamp()
        };
    }

     async addNewItems(count = 1) {
        const results = [];
        
        console.log(`🔄 Processing ${count} detection${count > 1 ? 's' : ''}...`);
        
        for (let i = 0; i < count; i++) {
            const dummyTrashItem = this.generateDummyTrashItem();
            const result = await this.postToRealAPI(dummyTrashItem);
            if (result) {
                results.push(result);
            }
            
            // Kortere delay voor grotere batches
            const delay = count > 5 ? 
                Math.floor(Math.random() * 50) + 25 :  // 25-75ms voor grote batches
                Math.floor(Math.random() * 150) + 50;  // 50-200ms voor kleine batches
            
            await new Promise(resolve => setTimeout(resolve, delay));
        }
        
        return results;
    }

    async postToRealAPI(dummyTrashItem) {
        try {
            const response = await this.authService.makeAuthenticatedRequest(this.realApiUrl, {
                method: 'POST',
                body: JSON.stringify(dummyTrashItem)
            });

            if (response.ok) {
                const result = await response.json();
                const timestamp = new Date(dummyTrashItem.Timestamp).toLocaleTimeString();
                const now = new Date().toLocaleTimeString();
                console.log(`✅ [${now}] ${dummyTrashItem.LitterType} detected at (${dummyTrashItem.Latitude}, ${dummyTrashItem.Longitude}) - recorded at ${timestamp}`);
                return result;
            } else {
                const errorText = await response.text();
                console.error(`❌ Failed to post: ${response.status} ${response.statusText} - ${errorText}`);
                return null;
            }
        } catch (error) {
            console.error('❌ Error posting to real API:', error.message);
            return null;
        }
    }

    // Verwijder oude methods
    async loadExistingData() {
        return [];
    }

    async saveData(items) {
        console.log(`Data wordt opgeslagen in echte database via API calls`);
    }
}

module.exports = TrashDataGenerator;