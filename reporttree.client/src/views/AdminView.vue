<template>
    <div class="admin-view">
        <h1>Administration</h1>
        <div class="admin-panels">
            <UsersManager />
            <GroupsManager />
        </div>
    </div>
</template>

<script setup lang="ts">
import UsersManager from '../components/Admin/UsersManager.vue'
import GroupsManager from '../components/Admin/GroupsManager.vue'
</script>

<style scoped>
.admin-panels {
    display: flex;
    gap: 24px;
    align-items: flex-start;
}

.admin-view h1 {
    margin-bottom: 16px;
}
</style>
