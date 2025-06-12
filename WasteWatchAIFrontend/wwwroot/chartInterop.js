export function setupLitterTypeChart(labels, data) {
    const ctx = document.getElementById('litterTypeChart').getContext('2d');

    return new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Aantal items',
                data: data,
                backgroundColor: [
                    'rgba(255, 243, 107, 0.7)',  
                    'rgba(54, 162, 235, 0.7)',  
                    'rgba(255, 206, 86, 0.7)',   
                    'rgba(75, 192, 192, 0.7)',   
                ],
                borderColor: [
                    'rgba(255, 107, 107, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(153, 102, 255, 1)',
                    'rgba(255, 159, 64, 1)',
                    'rgba(87, 206, 235, 1)',
                    'rgba(255, 99, 132, 1)'
                ],
                borderWidth: 1
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