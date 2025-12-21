<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api } from '../services/api'
import { useToastStore } from '../stores/toast'
import { User20, Password20 } from '@carbon/icons-vue'
import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/tile/index.js'
import '@carbon/web-components/es/components/tag/index.js'

interface UserProfile {
    username: string
    email: string
    roles: string[]
    groups: string[]
}

const toastStore = useToastStore()
const profile = ref<UserProfile | null>(null)
const loading = ref(false)
const showPasswordModal = ref(false)

const passwordForm = ref({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
})

const profileForm = ref({
    email: ''
})

async function loadProfile() {
    loading.value = true
    try {
        profile.value = await api.get<UserProfile>('/profile')
        profileForm.value.email = profile.value.email
    } catch (error) {
        console.error('Failed to load profile:', error)
    } finally {
        loading.value = false
    }
}

async function updateProfile() {
    try {
        await api.put('/profile', { email: profileForm.value.email })
        toastStore.success('Success', 'Profile updated successfully')
        await loadProfile()
    } catch (error) {
        console.error('Failed to update profile:', error)
    }
}

async function changePassword() {
    if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
        toastStore.error('Error', 'Passwords do not match')
        return
    }

    if (passwordForm.value.newPassword.length < 6) {
        toastStore.error('Error', 'Password must be at least 6 characters')
        return
    }

    try {
        await api.post('/profile/change-password', {
            currentPassword: passwordForm.value.currentPassword,
            newPassword: passwordForm.value.newPassword
        })
        toastStore.success('Success', 'Password changed successfully')
        showPasswordModal.value = false
        passwordForm.value = {
            currentPassword: '',
            newPassword: '',
            confirmPassword: ''
        }
    } catch (error) {
        console.error('Failed to change password:', error)
    }
}

function onEmailInput(e: Event) { profileForm.value.email = (e.target as HTMLInputElement).value }
function onCurrentPasswordInput(e: Event) { passwordForm.value.currentPassword = (e.target as HTMLInputElement).value }
function onNewPasswordInput(e: Event) { passwordForm.value.newPassword = (e.target as HTMLInputElement).value }
function onConfirmPasswordInput(e: Event) { passwordForm.value.confirmPassword = (e.target as HTMLInputElement).value }

onMounted(() => {
    loadProfile()
})
</script>

<template>
    <div class="user-profile">
        <h2>User Profile</h2>

        <div v-if="loading">Loading...</div>
        <div v-else-if="profile" class="profile-content">
            <cds-tile class="profile-card">
                <div class="profile-header">
                    <div class="avatar">
                        <User20 />
                    </div>
                    <div class="user-info">
                        <h3>{{ profile.username }}</h3>
                        <div class="roles">
                            <cds-tag v-for="role in profile.roles" :key="role" type="blue">{{ role }}</cds-tag>
                        </div>
                    </div>
                </div>

                <div class="profile-section">
                    <h4>Email</h4>
                    <cds-text-input label="Email Address" :value="profileForm.email" @input="onEmailInput"
                        placeholder="your.email@example.com"></cds-text-input>
                    <br />
                    <cds-button @click="updateProfile">Update Email</cds-button>
                </div>

                <div class="profile-section">
                    <h4>Groups</h4>
                    <div v-if="profile.groups.length > 0" class="groups">
                        <cds-tag v-for="group in profile.groups" :key="group" type="green">{{ group }}</cds-tag>
                    </div>
                    <p v-else class="no-data">Not a member of any groups</p>
                </div>

                <div class="profile-section">
                    <h4>Security</h4>
                    <cds-button kind="secondary" @click="showPasswordModal = true">
                        <Password20 slot="icon" />
                        Change Password
                    </cds-button>
                </div>
            </cds-tile>
        </div>

        <cds-modal :open="showPasswordModal" @cds-modal-closed="showPasswordModal = false">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>Security</cds-modal-label>
                <cds-modal-heading>Change Password</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <cds-text-input label="Current Password" type="password" :value="passwordForm.currentPassword"
                    @input="onCurrentPasswordInput" placeholder="Enter current password"></cds-text-input>
                <br />
                <cds-text-input label="New Password" type="password" :value="passwordForm.newPassword"
                    @input="onNewPasswordInput" placeholder="Enter new password"></cds-text-input>
                <br />
                <cds-text-input label="Confirm New Password" type="password" :value="passwordForm.confirmPassword"
                    @input="onConfirmPasswordInput" placeholder="Confirm new password"></cds-text-input>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary"
                    @click="showPasswordModal = false">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" @click="changePassword">Change
                    Password</cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>
    </div>
</template>

<style scoped lang="scss">
.user-profile {
    padding: 2rem;
    max-width: 800px;

    h2 {
        margin-bottom: 2rem;
        font-size: 1.5rem;
        color: var(--cds-text-primary);
    }
}

.profile-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.profile-card {
    padding: 2rem;
}

.profile-header {
    display: flex;
    align-items: center;
    gap: 1.5rem;
    margin-bottom: 2rem;
    padding-bottom: 2rem;
    border-bottom: 1px solid var(--cds-border-subtle-01);
}

.avatar {
    width: 64px;
    height: 64px;
    background: var(--cds-layer-accent-01);
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;

    svg {
        width: 32px;
        height: 32px;
        color: var(--cds-icon-primary);
    }
}

.user-info {
    h3 {
        margin: 0 0 0.5rem 0;
        font-size: 1.25rem;
        color: var(--cds-text-primary);
    }
}

.roles {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.profile-section {
    margin-bottom: 2rem;

    h4 {
        margin: 0 0 1rem 0;
        font-size: 1rem;
        color: var(--cds-text-primary);
    }
}

.groups {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.no-data {
    color: var(--cds-text-secondary);
    font-style: italic;
}
</style>
