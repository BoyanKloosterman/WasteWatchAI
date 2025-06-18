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

window.drawMonthlyTrendsChart = (data) => {
    // Destroy existing chart instance if it exists
    if (window.monthlyTrendsChartInstance) {
        window.monthlyTrendsChartInstance.destroy();
    }

    const ctx = document.getElementById('monthlyTrendsChart').getContext('2d');

    // Extract labels (month names) and data for each waste type
    const labels = data.map(item => item.monthName || item.maand || 'Unknown');
    const plasticData = data.map(item => item.plastic || 0);
    const papierData = data.map(item => item.papier || 0);
    const glasData = data.map(item => item.glas || 0);
    const organischData = data.map(item => item.organisch || 0);

    window.monthlyTrendsChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Plastic',
                    data: plasticData,
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    fill: false,
                    tension: 0.4,
                    borderWidth: 3,
                    pointRadius: 5,
                    pointBackgroundColor: '#0d6efd',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointHoverRadius: 7
                },
                {
                    label: 'Papier',
                    data: papierData,
                    borderColor: '#ffc107',
                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                    fill: false,
                    tension: 0.4,
                    borderWidth: 3,
                    pointRadius: 5,
                    pointBackgroundColor: '#ffc107',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointHoverRadius: 7
                },
                {
                    label: 'Glas',
                    data: glasData,
                    borderColor: '#20c997',
                    backgroundColor: 'rgba(32, 201, 151, 0.1)',
                    fill: false,
                    tension: 0.4,
                    borderWidth: 3,
                    pointRadius: 5,
                    pointBackgroundColor: '#20c997',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointHoverRadius: 7
                },
                {
                    label: 'Organisch',
                    data: organischData,
                    borderColor: '#fd7e14',
                    backgroundColor: 'rgba(253, 126, 20, 0.1)',
                    fill: false,
                    tension: 0.4,
                    borderWidth: 3,
                    pointRadius: 5,
                    pointBackgroundColor: '#fd7e14',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointHoverRadius: 7
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 20,
                        font: {
                            size: 12,
                            weight: 'bold'
                        }
                    }
                },
                title: {
                    display: true,
                    text: 'Afvaltrends per Type per Maand',
                    font: {
                        size: 16,
                        weight: 'bold'
                    },
                    padding: {
                        top: 10,
                        bottom: 30
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#ffffff',
                    bodyColor: '#ffffff',
                    borderColor: '#ffffff',
                    borderWidth: 1,
                    cornerRadius: 6,
                    displayColors: true,
                    callbacks: {
                        title: function (tooltipItems) {
                            return tooltipItems[0].label;
                        },
                        label: function (context) {
                            return `${context.dataset.label}: ${context.parsed.y} items`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Maanden',
                        font: {
                            size: 14,
                            weight: 'bold'
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)',
                        drawBorder: false
                    },
                    ticks: {
                        font: {
                            size: 11
                        },
                        maxRotation: 45,
                        minRotation: 0
                    }
                },
                y: {
                    display: true,
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Aantal Items',
                        font: {
                            size: 14,
                            weight: 'bold'
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)',
                        drawBorder: false
                    },
                    ticks: {
                        font: {
                            size: 11
                        },
                        stepSize: 1,
                        callback: function (value) {
                            return Number.isInteger(value) ? value : '';
                        }
                    }
                }
            },
            elements: {
                line: {
                    tension: 0.4
                },
                point: {
                    hoverRadius: 8
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeInOutQuart'
            }
        }
    });
};

window.drawTrashChart = (data) => {
    const ctx = document.getElementById('trashChart').getContext('2d');

    const labels = Object.keys(data).map(weekNum => 'Week ' + weekNum);
    const values = Object.values(data);

    if (window.trashChartInstance) {
        window.trashChartInstance.destroy();
    }

    window.trashChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Verandering Afval per week (%)',
                data: values,
                backgroundColor: 'rgba(54, 162, 235, 0.5)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return value + '%';
                        }
                    }
                }
            }
        }
    });
};

window.drawTrashChartMonthly = (data) => {
    if (window.trashChartMonthlyInstance) {
        window.trashChartMonthlyInstance.destroy();
    }

    const ctx = document.getElementById("trashChartMonthly").getContext("2d");
    const labels = Object.keys(data).map(k => {
        const year = Math.floor(k / 100);
        const month = k % 100;
        return new Date(year, month - 1).toLocaleString('nl-NL', { month: 'long', year: 'numeric' });
    });

    const values = Object.values(data);

    trashChartMonthlyInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: '% verandering t.o.v. vorige maand',
                data: values,
                backgroundColor: 'rgba(75, 192, 192, 0.6)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    title: { display: true, text: 'Percentage' },
                    ticks: {
                        callback: value => `${value}%`
                    }
                }
            }
        }
    });
}


window.exportAdvancedPDF = function (reportData) {
    try {
        // Check if jsPDF is available
        if (!window.jspdf || !window.jspdf.jsPDF) {
            console.error('jsPDF library not loaded');
            alert('PDF library not available. Please ensure jsPDF is loaded.');
            return false;
        }

        const { jsPDF } = window.jspdf;
        const doc = new jsPDF();

        // Color definitions
        const primaryColor = [0, 102, 204];
        const successColor = [25, 135, 84];
        const dangerColor = [220, 53, 69];
        const lightBlue = [240, 248, 255];
        const darkGray = [52, 58, 64];

        // Header section
        doc.setFillColor(...primaryColor);
        doc.rect(0, 0, 210, 30, 'F');
        doc.setTextColor(255, 255, 255);
        doc.setFontSize(24);
        doc.setFont(undefined, 'bold');
        doc.text('WasteWatch AI', 20, 20);
        doc.setFontSize(12);
        doc.setFont(undefined, 'normal');
        doc.text('Intelligent Waste Monitoring Report', 20, 26);
        doc.setTextColor(0, 0, 0);

        // Generation date
        doc.setFontSize(10);
        const generatedDate = reportData.generatedDate ?
            new Date(reportData.generatedDate).toLocaleDateString('nl-NL', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            }) :
            new Date().toLocaleDateString('nl-NL', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        doc.text(`Generated: ${generatedDate}`, 20, 40);

        // Summary section
        doc.setDrawColor(...primaryColor);
        doc.setFillColor(...lightBlue);
        doc.roundedRect(20, 50, 170, 35, 3, 3, 'FD');

        doc.setFontSize(16);
        doc.setFont(undefined, 'bold');
        doc.setTextColor(...darkGray);
        doc.text('Samenvatting', 25, 62);

        doc.setFontSize(12);
        doc.setFont(undefined, 'normal');
        doc.text('Gemiddelde verandering per week:', 25, 72);

        // Check if averageWeeklyChange exists
        const weeklyChange = reportData.averageWeeklyChange || 0;
        doc.setFontSize(18);
        doc.setFont(undefined, 'bold');
        const changeColor = weeklyChange >= 0 ? dangerColor : successColor;
        doc.setTextColor(...changeColor);
        doc.text(`${Math.abs(weeklyChange).toFixed(2)}%`, 25, 80);
        doc.setTextColor(0, 0, 0);
        doc.setFontSize(10);
        doc.setFont(undefined, 'normal');
        const trendText = weeklyChange >= 0 ? 'Toename in afval' : 'Afname in afval';
        doc.text(trendText, 120, 80);

        let currentY = 100;

        // Weekly data section
        doc.setFontSize(16);
        doc.setFont(undefined, 'bold');
        doc.text('Wekelijkse Data', 20, currentY);
        currentY += 20;

        // Check if weekly data exists and render chart or fallback
        if (reportData.weeklyData && reportData.weeklyData.length > 0) {
            if (typeof Chart !== 'undefined') {
                renderWeekChart();
            } else {
                // Fallback: render weekly data as text
                doc.setFontSize(12);
                doc.setFont(undefined, 'normal');
                doc.text('Grafiekbibliotheek niet beschikbaar', 20, currentY);
                currentY += 10;

                const recentWeeks = reportData.weeklyData.slice(-5);
                recentWeeks.forEach((week, index) => {
                    doc.text(`Week ${week.week}: ${week.proportionChange ? week.proportionChange.toFixed(2) : 0}%`, 20, currentY + (index * 8));
                });
                currentY += (recentWeeks.length * 8) + 20;
                renderMonthlyBenchmarkSection();
            }
        } else {
            doc.setFontSize(12);
            doc.text('Geen wekelijkse data beschikbaar', 20, currentY);
            currentY += 20;
            renderMonthlyBenchmarkSection();
        }

        function renderWeekChart() {
            try {
                const canvas = document.createElement('canvas');
                canvas.width = 850;
                canvas.height = 425;
                canvas.style.position = 'absolute';
                canvas.style.left = '-9999px';
                document.body.appendChild(canvas);

                const weekLabels = reportData.weeklyData.slice(-10).map(item => `Week ${item.week}`);
                const weekValues = reportData.weeklyData.slice(-10).map(item => item.proportionChange || 0);

                const chart = new Chart(canvas.getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: weekLabels,
                        datasets: [{
                            label: 'Verandering %',
                            data: weekValues,
                            backgroundColor: 'rgba(0, 123, 255, 0.3)',
                            borderColor: 'rgba(0, 123, 255, 1)',
                            borderWidth: 1
                        }]
                    },
                    options: {
                        animation: {
                            duration: 0,
                            onComplete: function () {
                                try {
                                    const img = canvas.toDataURL('image/png');
                                    doc.addImage(img, 'PNG', 20, currentY, 170, 85);
                                    document.body.removeChild(canvas);
                                    currentY += 100;
                                    renderMonthlyBenchmarkSection();
                                } catch (error) {
                                    console.error('Error rendering week chart:', error);
                                    document.body.removeChild(canvas);
                                    renderMonthlyBenchmarkSection();
                                }
                            }
                        },
                        responsive: false,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false }
                        },
                        scales: {
                            y: {
                                beginAtZero: true,
                                ticks: {
                                    callback: function (value) {
                                        return value + '%';
                                    }
                                }
                            }
                        }
                    }
                });
            } catch (error) {
                console.error('Error creating week chart:', error);
                renderMonthlyBenchmarkSection();
            }
        }

        // SWAPPED: Monthly benchmark section comes first now
        function renderMonthlyBenchmarkSection() {
            try {
                // Check if we need a new page
                if (currentY > 220) {
                    doc.addPage();
                    currentY = 20;
                }

                doc.setFontSize(16);
                doc.setFont(undefined, 'bold');
                doc.text('Maandelijkse Benchmark', 20, currentY);
                currentY += 15;

                // Check if monthly data exists
                if (reportData.monthlyBenchmark && reportData.monthlyBenchmark.length > 0) {
                    const monthlyTableData = reportData.monthlyBenchmark.slice(-6).map(item => {
                        const total = item.total || (item.plastic + item.papier + item.glas + item.organisch);
                        return [
                            item.monthName || item.maand || 'Unknown',
                            `${item.plastic || 0} (${total > 0 ? (((item.plastic || 0) / total) * 100).toFixed(1) : 0}%)`,
                            `${item.papier || 0} (${total > 0 ? (((item.papier || 0) / total) * 100).toFixed(1) : 0}%)`,
                            `${item.glas || 0} (${total > 0 ? (((item.glas || 0) / total) * 100).toFixed(1) : 0}%)`,
                            `${item.organisch || 0} (${total > 0 ? (((item.organisch || 0) / total) * 100).toFixed(1) : 0}%)`,
                            total.toString()
                        ];
                    });

                    // Check if autoTable is available
                    if (typeof doc.autoTable === 'function') {
                        doc.autoTable({
                            startY: currentY,
                            head: [['Maand', 'Plastic', 'Papier', 'Glas', 'Organisch', 'Totaal']],
                            body: monthlyTableData,
                            theme: 'striped',
                            headStyles: {
                                fillColor: [40, 167, 69],
                                textColor: [255, 255, 255],
                                fontSize: 10,
                                fontStyle: 'bold'
                            },
                            styles: {
                                fontSize: 9,
                                cellPadding: 3
                            },
                            alternateRowStyles: {
                                fillColor: [248, 249, 250]
                            },
                            margin: { left: 20, right: 20 }
                        });
                        currentY = doc.lastAutoTable.finalY + 20;
                    } else {
                        // Fallback: render as simple text
                        doc.setFontSize(10);
                        monthlyTableData.forEach((row, index) => {
                            doc.text(row.join(' | '), 20, currentY + (index * 8));
                        });
                        currentY += (monthlyTableData.length * 8) + 20;
                    }
                } else {
                    doc.setFontSize(12);
                    doc.text('Geen maandelijkse data beschikbaar', 20, currentY);
                    currentY += 20;
                }

                renderMonthlyDataSection();
            } catch (error) {
                console.error('Error rendering monthly benchmark section:', error);
                renderMonthlyDataSection();
            }
        }

        // SWAPPED: Monthly data chart section comes second now
        function renderMonthlyDataSection() {
            try {
                // Check if we need a new page
                if (currentY > 220) {
                    doc.addPage();
                    currentY = 20;
                }

                doc.setFontSize(16);
                doc.setFont(undefined, 'bold');
                doc.text('Maandelijkse Data', 20, currentY);
                currentY += 10;

                // Check if monthly data exists
                if (reportData.monthlyData && reportData.monthlyData.length > 0) {
                    if (typeof Chart !== 'undefined') {
                        renderMonthlyChart();
                    } else {
                        // Fallback: render monthly data as text
                        doc.setFontSize(12);
                        doc.setFont(undefined, 'normal');
                        doc.text('Grafiekbibliotheek niet beschikbaar', 20, currentY);
                        currentY += 10;

                        const recentMonths = reportData.monthlyData.slice(-6);
                        recentMonths.forEach((month, index) => {
                            doc.text(`${month.monthName}: ${month.proportionChange ? month.proportionChange.toFixed(2) : 0}%`, 20, currentY + (index * 8));
                        });
                        currentY += (recentMonths.length * 8) + 20;
                        renderPieChart();
                    }
                } else {
                    doc.setFontSize(12);
                    doc.text('Geen maandelijkse data beschikbaar', 20, currentY);
                    currentY += 20;
                    renderPieChart();
                }
            } catch (error) {
                console.error('Error rendering monthly data section:', error);
                renderPieChart();
            }
        }

        // Monthly chart rendering function
        function renderMonthlyChart() {
            try {
                const canvas = document.createElement('canvas');
                canvas.width = 850;
                canvas.height = 365;
                canvas.style.position = 'absolute';
                canvas.style.left = '-9999px';
                document.body.appendChild(canvas);

                const monthLabels = reportData.monthlyData.slice(-12).map(item => item.monthName);
                const monthValues = reportData.monthlyData.slice(-12).map(item => item.proportionChange || 0);

                const chart = new Chart(canvas.getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: monthLabels,
                        datasets: [{
                            label: 'Verandering %',
                            data: monthValues,
                            backgroundColor: 'rgba(40, 167, 69, 0.3)',
                            borderColor: 'rgba(40, 167, 69, 1)',
                            borderWidth: 1
                        }]
                    },
                    options: {
                        animation: {
                            duration: 0,
                            onComplete: function () {
                                try {
                                    const img = canvas.toDataURL('image/png');
                                    doc.addImage(img, 'PNG', 20, currentY, 170, 85);
                                    document.body.removeChild(canvas);
                                    currentY += 100;
                                    renderPieChart();
                                } catch (error) {
                                    console.error('Error rendering monthly chart:', error);
                                    document.body.removeChild(canvas);
                                    renderPieChart();
                                }
                            }
                        },
                        responsive: false,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            title: {
                                display: true,
                                text: 'Maandelijkse Verandering (%)',
                                font: { size: 14, weight: 'bold' }
                            }
                        },
                        scales: {
                            y: {
                                beginAtZero: true,
                                ticks: {
                                    callback: function (value) {
                                        return value + '%';
                                    }
                                }
                            },
                            x: {
                                ticks: {
                                    maxRotation: 45,
                                    minRotation: 45
                                }
                            }
                        }
                    }
                });
            } catch (error) {
                console.error('Error creating monthly chart:', error);
                renderPieChart();
            }
        }

        function renderPieChart() {
            try {
                if (currentY > 200) {
                    doc.addPage();
                    currentY = 30;
                }

                doc.setFontSize(16);
                doc.setFont(undefined, 'bold');
                doc.text('Verhouding Afvalsoorten', 20, currentY);
                currentY += 10;

                if (reportData.monthlyBenchmark && reportData.monthlyBenchmark.length > 0) {
                    const totalPlastic = reportData.monthlyBenchmark.reduce((sum, item) => sum + (item.plastic || 0), 0);
                    const totalPaper = reportData.monthlyBenchmark.reduce((sum, item) => sum + (item.papier || 0), 0);
                    const totalGlass = reportData.monthlyBenchmark.reduce((sum, item) => sum + (item.glas || 0), 0);
                    const totalOrganic = reportData.monthlyBenchmark.reduce((sum, item) => sum + (item.organisch || 0), 0);
                    const grandTotal = totalPlastic + totalPaper + totalGlass + totalOrganic;

                    if (typeof Chart !== 'undefined' && grandTotal > 0) {
                        const canvas = document.createElement('canvas');
                        canvas.width = 370;
                        canvas.height = 350;
                        canvas.style.position = 'absolute';
                        canvas.style.left = '-9999px';
                        document.body.appendChild(canvas);

                        const chart = new Chart(canvas.getContext('2d'), {
                            type: 'pie',
                            data: {
                                labels: ['Plastic', 'Papier', 'Glas', 'Organisch'],
                                datasets: [{
                                    data: [totalPlastic, totalPaper, totalGlass, totalOrganic],
                                    backgroundColor: ['#0d6efd', '#ffc107', '#20c997', '#fd7e14']
                                }]
                            },
                            options: {
                                animation: {
                                    duration: 0,
                                    onComplete: function () {
                                        try {
                                            const img = canvas.toDataURL('image/png');
                                            doc.addImage(img, 'PNG', 20, currentY, 150, 150);
                                            document.body.removeChild(canvas);
                                            renderMonthlyTrendsChart();
                                        } catch (error) {
                                            console.error('Error rendering pie chart:', error);
                                            document.body.removeChild(canvas);
                                            renderMonthlyTrendsChart();
                                        }
                                    }
                                },
                                responsive: false,
                                maintainAspectRatio: false,
                                plugins: {
                                    legend: {
                                        position: 'bottom'
                                    }
                                }
                            }
                        });
                    } else {
                        // Fallback: render as text
                        doc.setFontSize(12);
                        if (grandTotal > 0) {
                            doc.text(`Plastic: ${totalPlastic} (${((totalPlastic / grandTotal) * 100).toFixed(1)}%)`, 20, currentY);
                            doc.text(`Papier: ${totalPaper} (${((totalPaper / grandTotal) * 100).toFixed(1)}%)`, 20, currentY + 15);
                            doc.text(`Glas: ${totalGlass} (${((totalGlass / grandTotal) * 100).toFixed(1)}%)`, 20, currentY + 30);
                            doc.text(`Organisch: ${totalOrganic} (${((totalOrganic / grandTotal) * 100).toFixed(1)}%)`, 20, currentY + 45);
                        } else {
                            doc.text('Geen data beschikbaar voor verhouding', 20, currentY);
                        }
                        renderMonthlyTrendsChart();
                    }
                } else {
                    doc.setFontSize(12);
                    doc.text('Geen data beschikbaar voor verhouding', 20, currentY);
                    renderMonthlyTrendsChart();
                }
            } catch (error) {
                console.error('Error rendering pie chart:', error);
                renderMonthlyTrendsChart();
            }
        }

        function renderMonthlyTrendsChart() {
            try {
                doc.addPage();
                const startY = 30;
                doc.setFontSize(16);
                doc.setFont(undefined, 'bold');
                doc.text('Maandelijkse Afvaltrends per Type', 20, startY);

                if (typeof Chart !== 'undefined' && reportData.monthlyBenchmark && reportData.monthlyBenchmark.length > 0) {
                    const canvas = document.createElement('canvas');
                    canvas.width = 850;
                    canvas.height = 500;
                    canvas.style.position = 'absolute';
                    canvas.style.left = '-9999px';
                    document.body.appendChild(canvas);

                    const monthLabels = reportData.monthlyBenchmark.slice(-12).map(item => item.monthName || item.maand || 'Unknown');
                    const plasticData = reportData.monthlyBenchmark.slice(-12).map(item => item.plastic || 0);
                    const papierData = reportData.monthlyBenchmark.slice(-12).map(item => item.papier || 0);
                    const glasData = reportData.monthlyBenchmark.slice(-12).map(item => item.glas || 0);
                    const organischData = reportData.monthlyBenchmark.slice(-12).map(item => item.organisch || 0);

                    const chart = new Chart(canvas.getContext('2d'), {
                        type: 'line',
                        data: {
                            labels: monthLabels,
                            datasets: [
                                {
                                    label: 'Plastic',
                                    data: plasticData,
                                    borderColor: '#0d6efd',
                                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                                    fill: false,
                                    tension: 0.4,
                                    borderWidth: 4,
                                    pointRadius: 6,
                                    pointBackgroundColor: '#0d6efd'
                                },
                                {
                                    label: 'Papier',
                                    data: papierData,
                                    borderColor: '#ffc107',
                                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                                    fill: false,
                                    tension: 0.4,
                                    borderWidth: 4,
                                    pointRadius: 6,
                                    pointBackgroundColor: '#ffc107'
                                },
                                {
                                    label: 'Glas',
                                    data: glasData,
                                    borderColor: '#20c997',
                                    backgroundColor: 'rgba(32, 201, 151, 0.1)',
                                    fill: false,
                                    tension: 0.4,
                                    borderWidth: 4,
                                    pointRadius: 6,
                                    pointBackgroundColor: '#20c997'
                                },
                                {
                                    label: 'Organisch',
                                    data: organischData,
                                    borderColor: '#fd7e14',
                                    backgroundColor: 'rgba(253, 126, 20, 0.1)',
                                    fill: false,
                                    tension: 0.4,
                                    borderWidth: 4,
                                    pointRadius: 6,
                                    pointBackgroundColor: '#fd7e14'
                                }
                            ]
                        },
                        options: {
                            animation: {
                                duration: 0,
                                onComplete: function () {
                                    try {
                                        const img = canvas.toDataURL('image/png', 1.0);
                                        doc.addImage(img, 'PNG', 20, startY + 10, 170, 100);
                                        document.body.removeChild(canvas);
                                        finalizePDF();
                                    } catch (error) {
                                        console.error('Error rendering monthly trends chart:', error);
                                        document.body.removeChild(canvas);
                                        finalizePDF();
                                    }
                                }
                            },
                            responsive: false,
                            maintainAspectRatio: false,
                            plugins: {
                                legend: {
                                    position: 'top',
                                    labels: {
                                        usePointStyle: true,
                                        padding: 25,
                                        font: { size: 16 }
                                    }
                                },
                                title: {
                                    display: true,
                                    text: 'Trend per Afvaltype',
                                    font: { size: 18, weight: 'bold' }
                                }
                            },
                            scales: {
                                y: {
                                    beginAtZero: true,
                                    title: {
                                        display: true,
                                        text: 'Hoeveelheid',
                                        font: { size: 16 }
                                    },
                                    grid: { color: 'rgba(0,0,0,0.1)' },
                                    ticks: { font: { size: 14 } }
                                },
                                x: {
                                    title: {
                                        display: true,
                                        text: 'Maanden',
                                        font: { size: 16 }
                                    },
                                    grid: { color: 'rgba(0,0,0,0.1)' },
                                    ticks: { font: { size: 14 } }
                                }
                            }
                        }
                    });
                } else {
                    doc.setFontSize(12);
                    doc.text('Chart.js niet beschikbaar of geen maandelijkse data', 20, startY + 20);
                    finalizePDF();
                }
            } catch (error) {
                console.error('Error rendering monthly trends chart:', error);
                finalizePDF();
            }
        }

        function finalizePDF() {
            try {
                const pageCount = doc.internal.getNumberOfPages();
                for (let i = 1; i <= pageCount; i++) {
                    doc.setPage(i);
                    doc.setDrawColor(200, 200, 200);
                    doc.line(20, 285, 190, 285);
                    doc.setFontSize(8);
                    doc.setTextColor(100, 100, 100);
                    doc.text('WasteWatch AI - Intelligent Waste Monitoring System', 20, 290);
                    doc.text(`Pagina ${i} van ${pageCount}`, 170, 290);
                    doc.text(`Gegenereerd: ${new Date().toLocaleString('nl-NL')}`, 20, 294);
                }

                const fileName = `WasteWatch-Report-${new Date().toISOString().split('T')[0]}.pdf`;
                doc.save(fileName);
                console.log('Advanced PDF export completed:', fileName);
                return true;
            } catch (error) {
                console.error('Error finalizing PDF:', error);
                return false;
            }
        }

    } catch (error) {
        console.error('Error creating advanced PDF:', error);
        alert('Er is een fout opgetreden bij het genereren van de PDF: ' + error.message);
        return false;
    }
};