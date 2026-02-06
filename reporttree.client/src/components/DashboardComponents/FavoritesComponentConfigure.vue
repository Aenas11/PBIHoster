<script setup lang="ts">
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/toggle/index.js'
import type { ComponentConfigProps } from '../../types/components'
import { ref, watch } from 'vue'

const props = defineProps<ComponentConfigProps>()
const emit = defineEmits<{
    'update:modelValue': [value: Record<string, unknown>]
}>()

const showFavorites = ref(props.modelValue.showFavorites !== false)
const showRecents = ref(props.modelValue.showRecents !== false)
const maxItems = ref(String(props.modelValue.maxItems ?? 6))

watch([showFavorites, showRecents, maxItems], () => {
    emit('update:modelValue', {
        ...props.modelValue,
        showFavorites: showFavorites.value,
        showRecents: showRecents.value,
        maxItems: Number(maxItems.value || 6)
    })
})
</script>

<template>
    <div class="config-container">
        <cds-toggle :checked="showFavorites"
            @cds-toggle-changed="(e: CustomEvent) => showFavorites = !!e.detail?.checked">
            Show favorites
        </cds-toggle>
        <cds-toggle :checked="showRecents" @cds-toggle-changed="(e: CustomEvent) => showRecents = !!e.detail?.checked">
            Show recents
        </cds-toggle>
        <cds-text-input label="Max items" type="number" :value="maxItems"
            @input="maxItems = ($event.target as HTMLInputElement).value" min="1" max="20" />
    </div>
</template>

<style scoped>
.config-container {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
</style>
