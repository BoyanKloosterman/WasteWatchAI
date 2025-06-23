const fs = require('fs');
const path = require('path');

class TrashDataGenerator {
    constructor() {
        this.litterTypes = ['Plastic', 'Paper', 'Organic', 'Glass'];
        this.currentId = 1;
        this.realApiUrl = 'http://localhost:8080/api/TrashItems'; // Je echte API
        
        // Breda coordinate ranges voor realistische locaties
        this.locations = [
            { name: 'Grote Markt', lat: [51.5890, 51.5900], lng: [4.7750, 4.7765] },
            { name: 'Centraal Station', lat: [51.5953, 51.5963], lng: [4.7787, 4.7797] },
            { name: 'Valkenberg Park', lat: [51.5929, 51.5939], lng: [4.7791, 4.7801] },
            { name: 'Havermarkt', lat: [51.5920, 51.5925], lng: [4.7685, 4.7695] },
            { name: 'Wilhelminapark', lat: [51.5860, 51.5866], lng: [4.7848, 4.7856] }
        ];
        
        // Realistische tijd patronen
        this.timePatterns = {
            morning: { weight: 0.3, hours: [6, 7, 8, 9, 10, 11] },
            afternoon: { weight: 0.4, hours: [12, 13, 14, 15, 16, 17] },
            evening: { weight: 0.25, hours: [18, 19, 20, 21, 22] },
            night: { weight: 0.05, hours: [23, 0, 1, 2, 3, 4, 5] }
        };
        
        // Afval type waarschijnlijkheden
        this.litterProbabilities = {
            'Plastic': 0.45,
            'Paper': 0.25,
            'Organic': 0.20,
            'Glass': 0.10
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
        const daysBack = Math.floor(Math.random() * 30);
        const targetDate = new Date(now.getTime() - (daysBack * 24 * 60 * 60 * 1000));
        
        const rand = Math.random();
        let timePattern;
        
        if (rand < 0.3) timePattern = this.timePatterns.morning;
        else if (rand < 0.7) timePattern = this.timePatterns.afternoon;
        else if (rand < 0.95) timePattern = this.timePatterns.evening;
        else timePattern = this.timePatterns.night;
        
        const hour = timePattern.hours[Math.floor(Math.random() * timePattern.hours.length)];
        const minute = Math.floor(Math.random() * 60);
        const second = Math.floor(Math.random() * 60);
        
        targetDate.setHours(hour, minute, second, 0);
        return targetDate.toISOString();
    }

    generateTrashItem() {
        const location = this.getRandomLocation();
        return {
            LitterType: this.getRandomLitterType(),
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            Timestamp: this.getRandomTimestamp()
        };
    }

    async postToRealAPI(trashItem) {
        try {
            const response = await fetch(this.realApiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(trashItem)
            });

            if (response.ok) {
                const result = await response.json();
                console.log(`✅ Posted to database: ${trashItem.LitterType} at (${trashItem.Latitude}, ${trashItem.Longitude})`);
                return result;
            } else {
                console.error(`❌ Failed to post: ${response.status} ${response.statusText}`);
                return null;
            }
        } catch (error) {
            console.error('❌ Error posting to real API:', error.message);
            return null;
        }
    }

    async addNewItems(count = 5) {
        const results = [];
        
        for (let i = 0; i < count; i++) {
            const trashItem = this.generateTrashItem();
            const result = await this.postToRealAPI(trashItem);
            if (result) {
                results.push(result);
            }
            // Kleine delay tussen posts om database niet te overbelasten
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        
        return results;
    }

    // Fallback voor JSON bestand (voor backwards compatibility)
    async loadExistingData() {
        return []; // We gebruiken nu de echte database
    }

    async saveData(items) {
        // We saven niet meer naar JSON, maar naar echte database via API
        console.log(`Data wordt opgeslagen in echte database via API calls`);
    }
}

module.exports = TrashDataGenerator;