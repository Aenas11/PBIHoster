<template>
    <div class="panel analytics-dashboard">
        <div class="header">
            <div>
                <h2>Usage Analytics</h2>
                <p class="subtitle">Track adoption, engagement, and content popularity.</p>
            </div>
            <div class="actions">
                <cds-select label="Window" :value="String(days)" @cds-select-selected="onDaysChange">
                    <cds-select-item value="7">Last 7 days</cds-select-item>
                    <cds-select-item value="30">Last 30 days</cds-select-item>
                    <cds-select-item value="90">Last 90 days</cds-select-item>
                </cds-select>
                <cds-button kind="secondary" size="sm" @click="exportCsv">
                    Export CSV
                </cds-button>
                <cds-button kind="primary" size="sm" @click="load" :disabled="loading">
                    {{ loading ? 'Loading...' : 'Refresh' }}
                </cds-button>
            </div>
        </div>

        <div v-if="loading" class="loading-state">
            <cds-loading></cds-loading>
            <p>Loading analytics...</p>
        </div>

        <template v-else>
            <div class="kpi-grid">
                <article class="kpi-card">
                    <div class="kpi-label">Total Events</div>
                    <div class="kpi-value">{{ summary.totalEvents.toLocaleString() }}</div>
                </article>
                <article class="kpi-card">
                    <div class="kpi-label">Unique Users</div>
                    <div class="kpi-value">{{ summary.uniqueUsers.toLocaleString() }}</div>
                </article>
                <article class="kpi-card">
                    <div class="kpi-label">Top Event Type</div>
                    <div class="kpi-value">{{ topEventType }}</div>
                </article>
                <article class="kpi-card">
                    <div class="kpi-label">Most Viewed Path</div>
                    <div class="kpi-value path">{{ topPath }}</div>
                </article>
            </div>

            <div class="charts-grid">
                <section class="chart-card chart-card--wide">
                    <h3>Daily Activity</h3>
                    <div v-if="summary.dailySeries.length === 0" class="empty-state">No activity data yet.</div>
                    <div v-else class="sparkline-container">
                        <svg class="sparkline" :viewBox="`0 0 ${sparklineWidth} ${sparklineHeight}`" preserveAspectRatio="none">
                            <polyline
                                class="sparkline-line sparkline-line--total"
                                :points="totalPoints"
                                fill="none"
                            />
                            <polyline
                                class="sparkline-line sparkline-line--pageview"
                                :points="pageViewPoints"
                                fill="none"
                            />
                        </svg>
                        <div class="sparkline-legend">
                            <span class="legend-dot legend-dot--total"></span> Total events
                            <span class="legend-dot legend-dot--pageview"></span> Page views
                        </div>
                        <div class="sparkline-labels">
                            <span class="sparkline-label-start">{{ firstDate }}</span>
                            <span class="sparkline-label-end">{{ lastDate }}</span>
                        </div>
                    </div>
                </section>

                <section class="chart-card">
                    <h3>Event Type Mix</h3>
                    <div v-if="summary.eventTypes.length === 0" class="empty-state">No event data yet.</div>
                    <div v-else class="bar-list">
                        <div class="bar-item" v-for="event in summary.eventTypes" :key="event.eventType">
                            <div class="bar-meta">
                                <span>{{ event.eventType }}</span>
                                <strong>{{ event.count }}</strong>
                            </div>
                            <div class="bar-track">
                                <div class="bar-fill" :style="{ width: percent(event.count, maxEventCount) }"></div>
                            </div>
                        </div>
                    </div>
                </section>

                <section class="chart-card">
                    <h3>Top Paths</h3>
                    <div v-if="summary.topPaths.length === 0" class="empty-state">No path activity yet.</div>
                    <div v-else class="bar-list">
                        <div class="bar-item" v-for="item in summary.topPaths" :key="item.path">
                            <div class="bar-meta">
                                <span class="path">{{ item.path }}</span>
                                <strong>{{ item.count }}</strong>
                            </div>
                            <div class="bar-track">
                                <div class="bar-fill alt" :style="{ width: percent(item.count, maxPathCount) }"></div>
                            </div>
                        </div>
                    </div>
                </section>

                <section class="chart-card">
                    <h3>Device Types</h3>
                    <div v-if="summary.deviceTypes.length === 0" class="empty-state">No device data yet.</div>
                    <div v-else class="bar-list">
                        <div class="bar-item" v-for="device in summary.deviceTypes" :key="device.deviceType">
                            <div class="bar-meta">
                                <span>{{ device.deviceType }}</span>
                                <strong>{{ device.count }}</strong>
                            </div>
                            <div class="bar-track">
                                <div class="bar-fill device" :style="{ width: percent(device.count, maxDeviceCount) }"></div>
                            </div>
                        </div>
                    </div>
                </section>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getAnalyticsSummary, getAnalyticsExportUrl, type UsageSummaryResponse } from '@/services/analytics.service'
import { useToastStore } from '@/stores/toast'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/loading/index.js'

const toast = useToastStore()
const loading = ref(false)
const days = ref(30)

const sparklineWidth = 600
const sparklineHeight = 80
const sparklinePadding = 4

const summary = reactive<UsageSummaryResponse>({
    totalEvents: 0,
    uniqueUsers: 0,
    eventTypes: [],
    topPaths: [],
    dailySeries: [],
    deviceTypes: []
})

const maxEventCount = computed(() => Math.max(...summary.eventTypes.map(x => x.count), 1))
const maxPathCount = computed(() => Math.max(...summary.topPaths.map(x => x.count), 1))
const maxDeviceCount = computed(() => Math.max(...summary.deviceTypes.map(x => x.count), 1))

const topEventType = computed(() => {
    const first = summary.eventTypes[0]
    return first ? first.eventType : 'N/A'
})

const topPath = computed(() => {
    const first = summary.topPaths[0]
    return first ? first.path : 'N/A'
})

const firstDate = computed(() => summary.dailySeries[0]?.date?.slice(5) ?? '')
const lastDate = computed(() => summary.dailySeries[summary.dailySeries.length - 1]?.date?.slice(5) ?? '')

function buildSparklinePoints(values: number[]): string {
    if (values.length === 0) return ''
    const maxVal = Math.max(...values, 1)
    const n = values.length
    return values.map((v, i) => {
        const x = n === 1 ? sparklineWidth / 2 : (i / (n - 1)) * (sparklineWidth - sparklinePadding * 2) + sparklinePadding
        const y = sparklineHeight - sparklinePadding - (v / maxVal) * (sparklineHeight - sparklinePadding * 2)
        return `${x.toFixed(1)},${y.toFixed(1)}`
    }).join(' ')
}

const totalPoints = computed(() =>
    buildSparklinePoints(summary.dailySeries.map(d => d.totalEvents)))

const pageViewPoints = computed(() =>
    buildSparklinePoints(summary.dailySeries.map(d => d.pageViews)))

function percent(value: number, total: number): string {
    if (total <= 0) return '0%'
    const pct = Math.round((value / total) * 100)
    return `${Math.max(2, pct)}%`
}

function onDaysChange(event: CustomEvent<{ value?: string }>) {
    const selected = Number(event.detail?.value ?? '30')
    days.value = Number.isFinite(selected) ? selected : 30
    void load()
}

function exportCsv() {
    const url = getAnalyticsExportUrl(days.value)
    const a = document.createElement('a')
    a.href = url
    a.download = ''
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
}

async function load() {
    try {
        loading.value = true
        const data = await getAnalyticsSummary(days.value)
        summary.totalEvents = data.totalEvents
        summary.uniqueUsers = data.uniqueUsers
        summary.eventTypes = data.eventTypes
        summary.topPaths = data.topPaths
        summary.dailySeries = data.dailySeries ?? []
        summary.deviceTypes = data.deviceTypes ?? []
    } catch (error) {
        console.error('Failed to load analytics summary:', error)
        toast.error('Error', 'Failed to load analytics summary')
    } finally {
        loading.value = false
    }
}

onMounted(() => {
    void load()
})
</script>

<style scoped>
.analytics-dashboard {
    min-width: 360px;
    width: 100%;
}

.header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
}

.subtitle {
    color: var(--cds-text-secondary);
    margin-top: 0.25rem;
}

.actions {
    display: flex;
    align-items: flex-end;
    gap: 0.75rem;
}

.kpi-grid {
    margin-top: 1.25rem;
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 0.75rem;
}

.kpi-card {
    border: 1px solid var(--cds-border-subtle);
    background: var(--cds-layer);
    padding: 0.9rem;
}

.kpi-label {
    font-size: 0.75rem;
    color: var(--cds-text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.kpi-value {
    margin-top: 0.4rem;
    font-size: 1.3rem;
    font-weight: 600;
    color: var(--cds-text-primary);
}

.kpi-value.path {
    font-size: 0.95rem;
    overflow-wrap: anywhere;
}

.charts-grid {
    margin-top: 1rem;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
}

.chart-card {
    border: 1px solid var(--cds-border-subtle);
    background: var(--cds-layer);
    padding: 1rem;
}

.chart-card--wide {
    grid-column: 1 / -1;
}

.chart-card h3 {
    margin: 0 0 0.8rem;
}

.bar-list {
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.bar-item {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.bar-meta {
    display: flex;
    justify-content: space-between;
    gap: 0.5rem;
    font-size: 0.85rem;
}

.bar-meta .path {
    overflow-wrap: anywhere;
}

.bar-track {
    height: 8px;
    background: var(--cds-layer-accent);
}

.bar-fill {
    height: 100%;
    background: var(--cds-interactive);
}

.bar-fill.alt {
    background: var(--cds-support-info);
}

.bar-fill.device {
    background: var(--cds-support-success);
}

.sparkline-container {
    position: relative;
}

.sparkline {
    width: 100%;
    height: 80px;
    display: block;
}

.sparkline-line {
    stroke-width: 2;
    stroke-linecap: round;
    stroke-linejoin: round;
}

.sparkline-line--total {
    stroke: var(--cds-interactive);
}

.sparkline-line--pageview {
    stroke: var(--cds-support-info);
    stroke-dasharray: 4 2;
}

.sparkline-legend {
    margin-top: 0.4rem;
    font-size: 0.75rem;
    color: var(--cds-text-secondary);
    display: flex;
    align-items: center;
    gap: 1rem;
}

.legend-dot {
    display: inline-block;
    width: 10px;
    height: 3px;
    border-radius: 2px;
    margin-right: 4px;
    vertical-align: middle;
}

.legend-dot--total {
    background: var(--cds-interactive);
}

.legend-dot--pageview {
    background: var(--cds-support-info);
}

.sparkline-labels {
    display: flex;
    justify-content: space-between;
    font-size: 0.7rem;
    color: var(--cds-text-secondary);
    margin-top: 0.25rem;
}

.sparkline-label-start,
.sparkline-label-end {
    font-variant-numeric: tabular-nums;
}

.loading-state,
.empty-state {
    text-align: center;
    padding: 2rem 1rem;
    color: var(--cds-text-secondary);
}

@media (max-width: 1100px) {
    .kpi-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .charts-grid {
        grid-template-columns: 1fr;
    }

    .chart-card--wide {
        grid-column: 1;
    }
}

@media (max-width: 640px) {
    .header {
        flex-direction: column;
    }

    .actions {
        width: 100%;
        justify-content: flex-start;
    }

    .kpi-grid {
        grid-template-columns: 1fr;
    }
}
</style>
