let map;
let markersLayer;
let groteMarktLayer;
let centraalStationLayer;
let valkenbergParkLayer;
let haagdijkLayer;
let chasséParkLayer;
let chasséveldLayer;
let useHeatmap = false;


const litterTypeColors = {
    'Plastic': '#e74c3c',
    'Papier': '#3498db',
    'Glas': '#f39c12',
    'Organisch': '#2ecc71'
};

// Opstarten Kaart
window.initializeMap = function () {
    const ctx = document.getElementById('wasteMap');

    try {
        map = L.map('wasteMap').setView([51.5861, 4.7767], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        markersLayer = L.layerGroup().addTo(map);
    } catch { }
};

// Update Kaart
window.updateMapMarkers = function (jsonData) {
    try {
        // window.lastJsonData = jsonData;

        const trashData = JSON.parse(jsonData);

        if (!map || !markersLayer) {
            return;
        }

        if (!Array.isArray(trashData) || trashData.length === 0) {
            return;
        }

        markersLayer.clearLayers();

        if (groteMarktLayer) {
            map.removeLayer(groteMarktLayer);
            groteMarktLayer = null;
        }
        if (centraalStationLayer) {
            map.removeLayer(centraalStationLayer);
            centraalStationLayer = null;
        }
        if (valkenbergParkLayer) {
            map.removeLayer(valkenbergParkLayer);
            valkenbergParkLayer = null;
        }
        if (haagdijkLayer) {
            map.removeLayer(haagdijkLayer);
            haagdijkLayer = null;
        }
        if (chasséParkLayer) {
            map.removeLayer(chasséParkLayer);
            chasséParkLayer = null;
        }
        if (chasséveldLayer) {
            map.removeLayer(chasséveldLayer);
            chasséveldLayer = null;
        }

        if (useHeatmap) {
            const count = {};
            let maxCount = 0;
            let newRadius = 0;

            // Collecting the heatpoints
            const heatPoints = trashData
                .filter(item => typeof item.lat === 'number' && typeof item.lng === 'number')
                .map(item => {
                    const lat = item.lat + (Math.random() - 0.5) * 0.00002;
                    const lng = item.lng + (Math.random() - 0.5) * 0.00002;
                    return { lat, lng, intensity: 1, location: item.location };
                });

            // Counting totals
            heatPoints.forEach(item => {
                if (count[item.location]) {
                    count[item.location]++;
                } else {
                    count[item.location] = 1;
                }
            });

            // Checking the max
            for (const loc in count) {
                if (count[loc] > maxCount) {
                    maxCount = count[loc];
                }
            }

            // Grote Markt Breda
            const groteMarktPoints = heatPoints
                .filter(item => item.location == "Grote Markt Breda")
                .map(item => [item.lat, item.lng, 1]);

            newRadius = (groteMarktPoints.length / maxCount) * 18 + 2;

            groteMarktLayer = L.heatLayer(groteMarktPoints, {
                radius: newRadius,
                blur: 15,
                maxZoom: 15
            }).addTo(map);

            // Centraal Station Breda
            const centraalStationPoints = heatPoints
                .filter(item => item.location == "Centraal Station Breda")
                .map(item => [item.lat, item.lng, 1]);

            newRadius = (centraalStationPoints.length / maxCount) * 18 + 2;

            centraalStationLayer = L.heatLayer(centraalStationPoints, {
                radius: newRadius,
                blur: 15,
                maxZoom: 15
            }).addTo(map);


            // Valkenberg Park
            const valkenbergParkPoints = heatPoints
                .filter(item => item.location == "Valkenberg Park")
                .map(item => [item.lat, item.lng, 1]);

            newRadius = (valkenbergParkPoints.length / maxCount) * 18 + 2;

            valkenbergParkLayer = L.heatLayer(valkenbergParkPoints, {
                radius: newRadius,
                blur: 15,
                maxZoom: 15
            }).addTo(map);


            // Haagdijk
            const haagdijkPoints = heatPoints
                .filter(item => item.location == "Haagdijk")
                .map(item => [item.lat, item.lng, 1]);

            newRadius = (haagdijkPoints.length / maxCount) * 18 + 2;

            haagdijkLayer = L.heatLayer(haagdijkPoints, {
                radius: newRadius,
                blur: 15,
                maxZoom: 15
            }).addTo(map);


            // Chassé Park
            const chasséParkPoints = heatPoints
                .filter(item => item.location == "Chassé Park")
                .map(item => [item.lat, item.lng, 1]);

            newRadius = (chasséParkPoints.length / maxCount) * 18 + 2;

            chasséParkLayer = L.heatLayer(chasséParkPoints, {
                radius: newRadius,
                blur: 15,
                maxZoom: 15
            }).addTo(map);

            // Chasséveld
            const chasséveldPoints = heatPoints
                .filter(item => item.location == "Chasséveld")
                .map(item => [item.lat, item.lng, 1]);

            newRadius = (chasséveldPoints.length / maxCount) * 18 + 2;

            chasséveldLayer = L.heatLayer(chasséveldPoints, {
                radius: newRadius,
                blur: 15,
                maxZoom: 15
            }).addTo(map);


        }
        else {
            trashData.forEach((item, index) => {
                if (typeof item.lat !== 'number' || typeof item.lng !== 'number') {
                    return;
                }

                const color = litterTypeColors[item.litterType] || '#95a5a6';

                const lat = item.lat + (Math.random() - 0.5) * 0.00002;
                const lng = item.lng + (Math.random() - 0.5) * 0.00002;

                const marker = L.circleMarker([lat, lng], {
                    radius: 5,
                    fillColor: color,
                    color: '#fff',
                    weight: 1,
                    opacity: 1,
                    fillOpacity: 0.8
                });

                const popupContent = `
                        <div class="custom-popup">
                            <h6>${item.location || 'Onbekende locatie'}</h6>
                            <p><strong>Type:</strong> <span style="color:${color}">${item.litterType || 'Onbekend'}</span></p>
                            <p><strong>Tijd:</strong> ${item.timestamp || 'Onbekend'}</p>
                            <p><strong>Coördinaten:</strong> ${item.lat.toFixed(6)}, ${item.lng.toFixed(6)}</p>
                        </div>
                    `;

                marker.bindPopup(popupContent);
                markersLayer.addLayer(marker);
            });


            if (trashData.length > 1000) {
                map.setZoom(14);
            }
        }
    } catch { }
};

// Wisselen tussen normaal en heatmap
window.toggleHeatMap = function () {
    useHeatmap = !useHeatmap;
};