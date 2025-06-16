// Global chart variables
let typeDistributionChart = null;
let frequencyChart = null;
let correlationChart = null;
let weatherDistributionChart = null;
let scatterChart = null;
let predictionChart = null;

// Initialize tooltips on page load
document.addEventListener('DOMContentLoaded', function () {
    console.log('Chart.js loaded, Chart object available:', typeof Chart !== 'undefined');

    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// Type Distribution Chart
window.initializeTypeDistributionChart = function (chartData) {
    const ctx = document.getElementById('typeDistributionChart');
    if (!ctx) {
        console.error('typeDistributionChart canvas element not found');
        return;
    }

    // Destroy chart if no data
    if (!chartData || !chartData.labels || chartData.labels.length === 0) {
        if (typeDistributionChart) {
            typeDistributionChart.destroy();
            typeDistributionChart = null;
        }
        ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
        return;
    }

    if (typeDistributionChart) {
        typeDistributionChart.destroy();
    }

    try {
        typeDistributionChart = new Chart(ctx, {
            type: 'bar',
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: { display: false },
                    legend: { display: true, position: 'top' }
                },
                scales: {
                    x: { beginAtZero: true, grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' } },
                    y: { beginAtZero: true, grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' } }
                },
                interaction: { intersect: false, mode: 'index' }
            }
        });
        console.log('Type distribution chart initialized successfully');
    } catch (error) {
        console.error('Error initializing type distribution chart:', error);
    }
};

// Prediction Chart - Fixed implementation
window.initializePredictionChart = function (chartData) {
    // Debug logging
    console.log('initializePredictionChart called with data:', JSON.stringify(chartData, null, 2));

    const ctx = document.getElementById('predictionChart');
    if (!ctx) {
        console.error('predictionChart canvas element not found');
        return;
    }

    // Destroy chart if no data
    if (!chartData || !chartData.labels || chartData.labels.length === 0) {
        if (predictionChart) {
            predictionChart.destroy();
            predictionChart = null;
        }
        ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
        return;
    }

    if (predictionChart) {
        predictionChart.destroy();
    }

    try {
        predictionChart = new Chart(ctx, {
            type: 'bar',
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.datasets[0]?.label || 'Voorspelde afvalitems'
                    },
                    legend: {
                        labels: {
                            generateLabels: function (chart) {
                                const dataset = chart.data.datasets[0];
                                return chart.data.labels.map((label, i) => {
                                    return {
                                        text: label,
                                        fillStyle: dataset.backgroundColor[i],
                                        strokeStyle: dataset.borderColor[i],
                                        lineWidth: dataset.borderWidth,
                                        index: i
                                    };
                                });
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            title: function (tooltipItems) {
                                return tooltipItems[0].label;
                            },
                            label: function (context) {
                                return `Aantal: ${context.parsed.y}`;
                            },
                            afterLabel: function (context) {
                                // Return confidence if available
                                if (chartData.confidenceLabels && chartData.confidenceLabels[context.dataIndex]) {
                                    return chartData.confidenceLabels[context.dataIndex];
                                }
                                return '';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: { display: false },
                        ticks: {
                            color: '#333333',
                            font: {
                                weight: 'bold'
                            }
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' },
                        title: {
                            display: true,
                            text: 'Voorspeld aantal'
                        }
                    }
                },
                barPercentage: 0.7,       // Make bars wider
                categoryPercentage: 0.8,  // Reduce gap between categories
                layout: {
                    padding: {
                        top: 20,
                        right: 20,
                        bottom: 10,
                        left: 10
                    }
                }
            }
        });
        console.log('Prediction chart initialized successfully');
    } catch (error) {
        console.error('Error initializing prediction chart:', error);
    }
};



// Frequency Chart
window.initializeFrequencyChart = function (chartData) {
    const ctx = document.getElementById('frequencyChart');
    if (!ctx) {
        console.error('frequencyChart canvas element not found');
        return;
    }

    // Destroy chart if no data
    if (!chartData || !chartData.labels || chartData.labels.length === 0) {
        if (frequencyChart) {
            frequencyChart.destroy();
            frequencyChart = null;
        }
        ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
        return;
    }

    if (frequencyChart) {
        frequencyChart.destroy();
    }

    try {
        frequencyChart = new Chart(ctx, {
            type: 'line',
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: { display: false },
                    legend: { display: false }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' },
                        title: { display: true, text: 'Tijd van de dag' }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' },
                        title: { display: true, text: 'Aantal detecties' }
                    }
                },
                interaction: { intersect: false, mode: 'index' }
            }
        });
        console.log('Frequency chart initialized successfully');
    } catch (error) {
        console.error('Error initializing frequency chart:', error);
    }
};


// Correlation Chart (Main chart with temperature and trash count)
window.initializeCorrelationChart = function (chartData) {
    console.log('initializeCorrelationChart called with data:', chartData);

    const ctx = document.getElementById('correlationChart');
    if (!ctx) {
        console.error('correlationChart canvas element not found');
        return;
    }

    if (correlationChart) {
        correlationChart.destroy();
        console.log('Previous correlation chart destroyed');
    }

    try {
        correlationChart = new Chart(ctx, {
            type: 'bar', // Base type, will be mixed with line
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Relatie tussen Temperatuur en Afvalvolume',
                        font: { size: 16 }
                    },
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            label: function (context) {
                                let label = context.dataset.label || '';
                                if (label) label += ': ';

                                if (context.dataset.yAxisID === 'y') {
                                    label += context.parsed.y + '°C';
                                } else {
                                    label += context.parsed.y + ' items';
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' },
                        title: { display: true, text: 'Datum' }
                    },
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        grid: { display: true, color: 'rgba(255, 107, 107, 0.1)' },
                        title: { display: true, text: 'Temperatuur (°C)' },
                        beginAtZero: true
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        grid: { drawOnChartArea: false },
                        title: { display: true, text: 'Aantal Afval Items' },
                        beginAtZero: true
                    }
                },
                interaction: { intersect: false, mode: 'index' }
            }
        });

        console.log('Correlation chart initialized successfully');
    } catch (error) {
        console.error('Error initializing correlation chart:', error);
    }
};

// Weather Distribution Chart
window.initializeWeatherDistributionChart = function (chartData) {
    console.log('initializeWeatherDistributionChart called with data:', chartData);

    const ctx = document.getElementById('weatherDistributionChart');
    if (!ctx) {
        console.error('weatherDistributionChart canvas element not found');
        return;
    }

    if (weatherDistributionChart) {
        weatherDistributionChart.destroy();
    }

    try {
        weatherDistributionChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: chartData.labels,
                datasets: [{
                    data: chartData.values,
                    backgroundColor: [
                        '#ffd700', // Zonnig
                        '#87ceeb', // Bewolkt
                        '#d3d3d3', // Mistig
                        '#4169e1', // Regenachtig
                        '#ffffff', // Sneeuw
                        '#8b008b', // Onweer
                        '#696969'  // Overig
                    ],
                    borderColor: [
                        '#ffcc00', '#5f9ea0', '#a9a9a9', '#1e90ff',
                        '#f0f0f0', '#663399', '#2f4f4f'
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true, position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.parsed / total) * 100).toFixed(1);
                                return `${context.label}: ${context.parsed} dagen (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });

        console.log('Weather distribution chart initialized successfully');
    } catch (error) {
        console.error('Error initializing weather distribution chart:', error);
    }
};

// Scatter Chart
window.initializeScatterChart = function (chartData) {
    console.log('initializeScatterChart called with data:', chartData);

    const ctx = document.getElementById('scatterChart');
    if (!ctx) {
        console.error('scatterChart canvas element not found');
        return;
    }

    if (scatterChart) {
        scatterChart.destroy();
    }

    try {
        scatterChart = new Chart(ctx, {
            type: 'scatter',
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Correlatie: Temperatuur vs Afvalvolume'
                    },
                    legend: { display: true, position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return `Temp: ${context.parsed.x}°C, Afval: ${context.parsed.y} items`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        title: { display: true, text: 'Temperatuur (°C)' },
                        grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' }
                    },
                    y: {
                        title: { display: true, text: 'Aantal Afval Items' },
                        grid: { display: true, color: 'rgba(0, 0, 0, 0.1)' },
                        beginAtZero: true
                    }
                },
                interaction: { intersect: false, mode: 'point' }
            }
        });

        console.log('Scatter chart initialized successfully');
    } catch (error) {
        console.error('Error initializing scatter chart:', error);
    }
};

// Debug function to check if all functions are available
window.checkChartFunctions = function () {
    console.log('Chart functions available:');
    console.log('- initializePredictionChart:', typeof window.initializePredictionChart);
    console.log('- initializeTypeDistributionChart:', typeof window.initializeTypeDistributionChart);
    console.log('- initializeFrequencyChart:', typeof window.initializeFrequencyChart);
    console.log('- initializeCorrelationChart:', typeof window.initializeCorrelationChart);
    console.log('- initializeWeatherDistributionChart:', typeof window.initializeWeatherDistributionChart);
    console.log('- initializeScatterChart:', typeof window.initializeScatterChart);
    console.log('- Chart.js available:', typeof Chart !== 'undefined');
};
// Function to destroy all charts
window.destroyAllCharts = function () {
    console.log('Destroying all charts...');

    if (typeDistributionChart) {
        typeDistributionChart.destroy();
        typeDistributionChart = null;
    }

    if (frequencyChart) {
        frequencyChart.destroy();
        frequencyChart = null;
    }

    if (correlationChart) {
        correlationChart.destroy();
        correlationChart = null;
    }

    if (weatherDistributionChart) {
        weatherDistributionChart.destroy();
        weatherDistributionChart = null;
    }

    if (scatterChart) {
        scatterChart.destroy();
        scatterChart = null;
    }

    if (predictionChart) {
        predictionChart.destroy();
        predictionChart = null;
    }

    console.log('All charts destroyed successfully');
};
