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
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getAnalyticsSummary, type UsageSummaryResponse } from '@/services/analytics.service'
import { useToastStore } from '@/stores/toast'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/loading/index.js'

const toast = useToastStore()
const loading = ref(false)
const days = ref(30)

const summary = reactive<UsageSummaryResponse>({
    totalEvents: 0,
    uniqueUsers: 0,
    eventTypes: [],
    topPaths: []
})

const maxEventCount = computed(() => Math.max(...summary.eventTypes.map(x => x.count), 1))
const maxPathCount = computed(() => Math.max(...summary.topPaths.map(x => x.count), 1))

const topEventType = computed(() => {
    const first = summary.eventTypes[0]
    return first ? first.eventType : 'N/A'
})

const topPath = computed(() => {
    const first = summary.topPaths[0]
    return first ? first.path : 'N/A'
})

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

async function load() {
    try {
        loading.value = true
        const data = await getAnalyticsSummary(days.value)
        summary.totalEvents = data.totalEvents
        summary.uniqueUsers = data.uniqueUsers
        summary.eventTypes = data.eventTypes
        summary.topPaths = data.topPaths
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
