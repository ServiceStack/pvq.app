import ChartJs, { colors } from "../components/ChartJs.mjs"

export default {
    components: { ChartJs },
    template: `
        <ChartJs type="bar" :data="data" :options="options" style="width:1024px" />
    `,
    props:['results'],
    setup(props) {
        let results = props.results
        results.sort((a,b) => b.value - a.value)
        results = results.slice(0,20)
        
        const datasets = results.map((x,i) => ({
            label: x.displayName,
            backgroundColor: colors[i % colors.length].background,
            borderColor: colors[i % colors.length].border,
            borderWidth: 1,
            data: [x.value]
        }))
        
        const data = {
            labels: ['Votes'],
            datasets
        }
        
        const options = {
            responsive: true,
            plugins: {
                legend: {
                    position: "left"
                },
                title: {
                    display: true,
                    text: 'Total Votes for Top 1K Most Voted Questions',
                    font: {
                        size: 20,
                    }
                },
            },
        }
        
        return { data, options }
    }
}
