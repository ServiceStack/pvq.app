import ChartJs, { colors } from "../components/ChartJs.mjs"

export default {
    components: { ChartJs },
    template: `
        <ChartJs :data="data" :plugins="{ legend: { position: 'left' } }" />
    `,
    props:['results'],
    setup(props) {
        let results = props.results
        results.sort((a,b) => b.value - a.value)
        results = results.slice(0,10)

        const datasets = results.map((x,i) => ({
            label: x.displayName,
            backgroundColor: colors[i % colors.length].background,
            borderColor: colors[i % colors.length].border,
            borderWidth: 1,
            data: [x.value]
        }))

        const data = {
            labels: ['Win Rates %'],
            datasets
        }

        return { data }
    }
}
