<script setup lang="ts">
import { useToastStore } from '../stores/toast'
import { CheckmarkFilled20, ErrorFilled20, WarningAltFilled20, InformationFilled20, Close20 } from '@carbon/icons-vue'

const toastStore = useToastStore()

const getIcon = (type: string) => {
    switch (type) {
        case 'success': return CheckmarkFilled20
        case 'error': return ErrorFilled20
        case 'warning': return WarningAltFilled20
        case 'info': return InformationFilled20
        default: return InformationFilled20
    }
}

const getTypeClass = (type: string) => {
    switch (type) {
        case 'success': return 'toast--success'
        case 'error': return 'toast--error'
        case 'warning': return 'toast--warning'
        case 'info': return 'toast--info'
        default: return 'toast--info'
    }
}
</script>

<template>
    <div class="toast-container">
        <TransitionGroup name="toast">
            <div v-for="toast in toastStore.toasts" :key="toast.id" class="toast" :class="getTypeClass(toast.type)">
                <component :is="getIcon(toast.type)" class="toast__icon" />
                <div class="toast__content">
                    <div class="toast__title">{{ toast.title }}</div>
                    <div v-if="toast.message" class="toast__message">{{ toast.message }}</div>
                </div>
                <button class="toast__close" @click="toastStore.remove(toast.id)" aria-label="Close notification">
                    <Close20 />
                </button>
            </div>
        </TransitionGroup>
    </div>
</template>

<style scoped lang="scss">
.toast-container {
    position: fixed;
    top: 3rem;
    right: 1rem;
    z-index: 9999;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    pointer-events: none;
}

.toast {
    display: flex;
    align-items: flex-start;
    gap: 0.75rem;
    min-width: 320px;
    max-width: 400px;
    padding: 1rem;
    background: var(--cds-layer-01);
    border-left: 4px solid;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
    pointer-events: auto;

    &--success {
        border-left-color: var(--cds-support-success);

        .toast__icon {
            color: var(--cds-support-success);
        }
    }

    &--error {
        border-left-color: var(--cds-support-error);

        .toast__icon {
            color: var(--cds-support-error);
        }
    }

    &--warning {
        border-left-color: var(--cds-support-warning);

        .toast__icon {
            color: var(--cds-support-warning);
        }
    }

    &--info {
        border-left-color: var(--cds-support-info);

        .toast__icon {
            color: var(--cds-support-info);
        }
    }
}

.toast__icon {
    flex-shrink: 0;
    margin-top: 0.125rem;
}

.toast__content {
    flex: 1;
    min-width: 0;
}

.toast__title {
    font-weight: 600;
    color: var(--cds-text-primary);
    margin-bottom: 0.25rem;
}

.toast__message {
    font-size: 0.875rem;
    color: var(--cds-text-secondary);
}

.toast__close {
    flex-shrink: 0;
    background: none;
    border: none;
    padding: 0.25rem;
    cursor: pointer;
    color: var(--cds-icon-primary);
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 4px;

    &:hover {
        background: var(--cds-layer-hover-01);
    }

    &:active {
        background: var(--cds-layer-active-01);
    }
}

// Animations
.toast-enter-active,
.toast-leave-active {
    transition: all 0.3s ease;
}

.toast-enter-from {
    opacity: 0;
    transform: translateX(100%);
}

.toast-leave-to {
    opacity: 0;
    transform: translateX(100%);
}

.toast-move {
    transition: transform 0.3s ease;
}
</style>
