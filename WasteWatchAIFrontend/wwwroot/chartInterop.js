// Dezelfde kleuren als de map
const litterTypeColors = {
    'Plastic': '#e74c3c',
    'Papier': '#3498db',
    'Glas': '#f39c12',
    'Organisch': '#2ecc71'
};

export function setupLitterTypeChart(labels, data) {
    const ctx = document.getElementById('litterTypeChart').getContext('2d');

    // Genereer achtergrondkleuren gebaseerd op de labels (100% opacity)
    const backgroundColors = labels.map(label => {
        return litterTypeColors[label] || '#95a5a6';
    });

    return new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Aantal items',
                data: data,
                backgroundColor: backgroundColors,
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `${context.dataset.label}: ${context.raw}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    },
                    grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' }
                }
            }
        }
    });
}