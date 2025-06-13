// Global chart variables
let typeDistributionChart = null;
let frequencyChart = null;
let correlationChart = null;
let weatherDistributionChart = null;
let scatterChart = null;

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

    console.log('All charts destroyed successfully');
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
        const { jsPDF } = window.jspdf;
        const doc = new jsPDF();

        const primaryColor = [0, 102, 204];
        const successColor = [25, 135, 84];
        const dangerColor = [220, 53, 69];
        const lightBlue = [240, 248, 255];
        const darkGray = [52, 58, 64];

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

        doc.setFontSize(10);
        doc.text(`Generated: ${new Date(reportData.generatedDate).toLocaleDateString('nl-NL', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        })}`, 20, 40);

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

        doc.setFontSize(18);
        doc.setFont(undefined, 'bold');
        const changeColor = reportData.averageWeeklyChange >= 0 ? dangerColor : successColor;
        doc.setTextColor(...changeColor);
        doc.text(`${Math.abs(reportData.averageWeeklyChange).toFixed(2)}%`, 25, 80);
        doc.setTextColor(0, 0, 0);
        doc.setFontSize(10);
        doc.setFont(undefined, 'normal');
        const trendText = reportData.averageWeeklyChange >= 0 ? 'Toename in afval' : 'Afname in afval';
        doc.text(trendText, 120, 80);

        let startY = 100;
        doc.setFontSize(16);
        doc.setFont(undefined, 'bold');
        doc.text('Wekelijkse Data', 20, startY);

        let chartsToRender = 0;
        let chartsRendered = 0;
        let weekChartY = startY + 10; // Position directly under "Wekelijkse Data"
        let weekChartRendered = false;

        const checkFinalize = () => {
            chartsRendered++;
            if (chartsRendered === chartsToRender) finalizePDF();
        };

        const renderWeekChart = () => {
            if (typeof Chart !== 'undefined' && !weekChartRendered) {
                weekChartRendered = true;
                chartsToRender++;
                const canvasLine = document.createElement('canvas');
                canvasLine.width = 850;
                canvasLine.height = 425;
                document.body.appendChild(canvasLine);

                const weekLabels = reportData.weeklyData.slice(-10).map(item => `Week ${item.week}`);
                const weekValues = reportData.weeklyData.slice(-10).map(item => item.proportionChange);

                new Chart(canvasLine.getContext('2d'), {
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
                            onComplete: () => {
                                const img = canvasLine.toDataURL('image/png');
                                doc.addImage(img, 'PNG', 20, weekChartY, 170, 85);
                                setTimeout(() => document.body.removeChild(canvasLine), 0);
                                renderMonthlySection();
                                checkFinalize();
                            }
                        },
                        responsive: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { beginAtZero: true, ticks: { callback: v => `${v}%` } }
                        }
                    }
                });
            } else {
                doc.setFontSize(12);
                doc.text('Grafiekbibliotheek niet beschikbaar - tabel wordt weergegeven', 20, weekChartY);
                renderMonthlySection();
            }
        };

        const renderMonthlySection = () => {
            // Monthly benchmark starts after the week chart
            const monthlyStartY = weekChartY + 100;

            doc.setFontSize(16);
            doc.setFont(undefined, 'bold');
            doc.text('Maandelijkse Benchmark', 20, monthlyStartY);

            const monthlyTableData = reportData.monthlyBenchmark.slice(-6).map(item => {
                const total = item.total;
                return [
                    item.monthName,
                    `${item.plastic} (${((item.plastic / total) * 100).toFixed(1)}%)`,
                    `${item.papier} (${((item.papier / total) * 100).toFixed(1)}%)`,
                    `${item.glas} (${((item.glas / total) * 100).toFixed(1)}%)`,
                    `${item.organisch} (${((item.organisch / total) * 100).toFixed(1)}%)`,
                    total.toString()
                ];
            });

            doc.autoTable({
                startY: monthlyStartY + 10,
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
                margin: { left: 20, right: 20 },
                columnStyles: {
                    0: { cellWidth: 35 },
                    1: { halign: 'center', cellWidth: 28 },
                    2: { halign: 'center', cellWidth: 28 },
                    3: { halign: 'center', cellWidth: 28 },
                    4: { halign: 'center', cellWidth: 28 },
                    5: { halign: 'center', cellWidth: 25, fontStyle: 'bold' }
                }
            });

            renderPieChart();
        };

        const renderPieChart = () => {
            // Pie chart starts after the monthly table
            let pieStartY = doc.lastAutoTable?.finalY > 220 ? (doc.addPage(), 30) : (doc.lastAutoTable?.finalY ?? 280) + 25;
            doc.setFontSize(16);
            doc.setFont(undefined, 'bold');
            doc.text('Verhouding Afvalsoorten', 20, pieStartY);

            if (typeof Chart !== 'undefined') {
                chartsToRender++;
                const canvasPie = document.createElement('canvas');
                canvasPie.width = 400;
                canvasPie.height = 400;
                document.body.appendChild(canvasPie);

                const totalPlastic = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.plastic, 0);
                const totalPaper = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.papier, 0);
                const totalGlass = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.glas, 0);
                const totalOrganic = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.organisch, 0);

                new Chart(canvasPie.getContext('2d'), {
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
                            onComplete: () => {
                                const img = canvasPie.toDataURL('image/png');
                                doc.addImage(img, 'PNG', 20, pieStartY + 10, 150, 150);
                                setTimeout(() => document.body.removeChild(canvasPie), 0);
                                checkFinalize();
                            }
                        },
                        plugins: {
                            legend: {
                                position: 'bottom',
                                labels: {
                                    font: {
                                        size: 60
                                    }
                                }
                            }
                        }
                    }
                });
            } else {
                const totalPlastic = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.plastic, 0);
                const totalPaper = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.papier, 0);
                const totalGlass = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.glas, 0);
                const totalOrganic = reportData.monthlyBenchmark.reduce((sum, item) => sum + item.organisch, 0);
                const grandTotal = totalPlastic + totalPaper + totalGlass + totalOrganic;

                doc.setFontSize(12);
                doc.text(`Plastic: ${totalPlastic} (${((totalPlastic / grandTotal) * 100).toFixed(1)}%)`, 20, pieStartY + 20);
                doc.text(`Papier: ${totalPaper} (${((totalPaper / grandTotal) * 100).toFixed(1)}%)`, 20, pieStartY + 35);
                doc.text(`Glas: ${totalGlass} (${((totalGlass / grandTotal) * 100).toFixed(1)}%)`, 20, pieStartY + 50);
                doc.text(`Organisch: ${totalOrganic} (${((totalOrganic / grandTotal) * 100).toFixed(1)}%)`, 20, pieStartY + 65);

                if (typeof Chart === 'undefined') finalizePDF();
            }
        };

        // Start the rendering process
        renderWeekChart();

        function finalizePDF() {
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
        }

        return true;
    } catch (error) {
        console.error('Error creating advanced PDF:', error);
        return false;
    }
};
