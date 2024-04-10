import { ref, computed, watch, watchEffect, nextTick, onMounted, onUpdated, getCurrentInstance  } from "vue"
import { $$, $1, on, JsonServiceClient, EventBus, toDate, humanize, leftPart, lastRightPart } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { mount, alreadyMounted, forceMount } from "app.mjs"
import {GetMeta} from "./dtos.mjs";
import Chart from "../posts/components/ChartJs.mjs";

export default  {
    async load() {
        const el = $1("#winrate-chart")
        if (!el) return
        
        // Get data prop values from data attributes
        const labelsArray = el.getAttribute("data-labels")
        const datasetsArray = el.getAttribute("data-datasets")
        const optionsObj = el.getAttribute("data-options")
        
        // Get type prop value from data attribute
        const type = "bar"
        
        console.log({labelsArray, datasetsArray, optionsObj, type})
        
        // Parse data attributes
        const labels = JSON.parse(labelsArray)
        const datasets = JSON.parse(datasetsArray)
        const options = JSON.parse(optionsObj)
        
        mount(el, Chart, { type: type, data: {labels: labels, datasets: datasets}, options: options })
    }
}