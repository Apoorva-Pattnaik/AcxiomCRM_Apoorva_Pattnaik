// AcxiomCRM Dashboard Chart.js Initializer

function initDashboardCharts(leadData, pipelineData, monthlySalesData) {
    // 1. Lead Status Chart (Doughnut)
    var leadCanvas = document.getElementById('leadStatusChart');
    if (leadCanvas) {
        var leadLabels = leadData.map(d => d.label);
        var leadCounts = leadData.map(d => d.count);
        var leadColors = leadData.map(d => d.color);

        new Chart(leadCanvas, {
            type: 'doughnut',
            data: {
                labels: leadLabels,
                datasets: [{
                    data: leadCounts,
                    backgroundColor: leadColors,
                    borderWidth: 2,
                    borderColor: '#FFFFFF'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            boxWidth: 12,
                            padding: 15,
                            font: { family: "'Plus Jakarta Sans', sans-serif", size: 12 }
                        }
                    }
                },
                cutout: '65%'
            }
        });
    }

    // 2. Opportunity Pipeline Chart (Bar)
    var pipelineCanvas = document.getElementById('pipelineChart');
    if (pipelineCanvas) {
        var pipeLabels = pipelineData.map(d => d.label);
        var pipeValues = pipelineData.map(d => d.value);
        var pipeColors = pipelineData.map(d => d.color);

        new Chart(pipelineCanvas, {
            type: 'bar',
            data: {
                labels: pipeLabels,
                datasets: [{
                    label: 'Pipeline Value (₹)',
                    data: pipeValues,
                    backgroundColor: pipeColors,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return '₹' + (value >= 1000 ? (value / 1000) + 'k' : value);
                            },
                            font: { family: "'Plus Jakarta Sans', sans-serif" }
                        },
                        grid: { color: '#F1F5F9' }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: "'Plus Jakarta Sans', sans-serif" } }
                    }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return ' ' + new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(context.raw);
                            }
                        }
                    }
                }
            }
        });
    }

    // 3. Monthly Sales Chart (Line)
    var salesCanvas = document.getElementById('monthlySalesChart');
    if (salesCanvas) {
        var months = monthlySalesData.map(d => d.month);
        var wonAmounts = monthlySalesData.map(d => d.wonAmount);

        new Chart(salesCanvas, {
            type: 'line',
            data: {
                labels: months,
                datasets: [{
                    label: 'Revenue Won (₹)',
                    data: wonAmounts,
                    borderColor: '#10B981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.35,
                    pointBackgroundColor: '#10B981',
                    pointRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return '₹' + (value >= 1000 ? (value / 1000) + 'k' : value);
                            },
                            font: { family: "'Plus Jakarta Sans', sans-serif" }
                        },
                        grid: { color: '#F1F5F9' }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: "'Plus Jakarta Sans', sans-serif" } }
                    }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return ' Won Revenue: ' + new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(context.raw);
                            }
                        }
                    }
                }
            }
        });
    }
}
