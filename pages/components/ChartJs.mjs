import { ref, onMounted } from "vue"
import { addScript } from "@servicestack/client"

const loadJs = addScript('/lib/js/chart.umd.min.js')

export const colors = [
    { background: 'rgba(201, 203, 207, 0.2)', border: 'rgb(201, 203, 207)' },
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(153, 102, 255, 0.2)', border: 'rgb(153, 102, 255)' },
    { background: 'rgba(54, 162, 235, 0.2)',  border: 'rgb(54, 162, 235)' },
    { background: 'rgba(255, 159, 64, 0.2)',  border: 'rgb(255, 159, 64)' },
    { background: 'rgba(67, 56, 202, 0.2)',   border: 'rgb(67, 56, 202)' },
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(14, 116, 144, 0.2)',  border: 'rgb(14, 116, 144)' },
    { background: 'rgba(162, 28, 175, 0.2)',  border: 'rgb(162, 28, 175)' },
]

export default {
    template:`<canvas ref="chart"></canvas>`,
    props:['type','data','options','plugins'],
    setup(props) {
        const chart = ref()
        onMounted(async () => {
            await loadJs

            const options = props.options || {
                responsive: true,
                plugins: props.plugins || {
                    legend: {
                        position: "bottom"
                    },
                },
            }
            new Chart(chart.value, {
                type: props.type || "bar",
                data: props.data,
                options,
                plugins: props.plugins ?? []
            })

        })
        return { chart }
    }
}
