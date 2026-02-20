<script setup lang="ts">
import '@carbon/web-components/es/components/tabs/index.js'
import UsersManager from '../components/Admin/UsersManager.vue'
import GroupsManager from '../components/Admin/GroupsManager.vue'
import SettingsManager from '../components/Admin/SettingsManager.vue'
import PowerBIDiagnosticsCard from '../components/Admin/PowerBIDiagnosticsCard.vue'
import DataRefreshManager from '../components/Admin/DataRefreshManager.vue'
import AuditLogsPanel from '../components/Admin/AuditLogsPanel.vue'
import RLSManager from '../components/Admin/RLSManager.vue'
</script>

<template>
    <div class="admin-view">
        <h1>Administration</h1>

        <cds-tabs value="tab-users">
            <cds-tab id="tab-users" target="panel-users" value="tab-users">Users & Groups</cds-tab>
            <cds-tab id="tab-settings" target="panel-settings" value="tab-settings">Settings</cds-tab>
            <cds-tab id="tab-rls" target="panel-rls" value="tab-rls">RLS Management</cds-tab>
            <cds-tab id="tab-audit" target="panel-audit" value="tab-audit">Audit Logs</cds-tab>
        </cds-tabs>

        <div id="panel-users" role="tabpanel" aria-labelledby="tab-users">
            <div class="admin-panels">
                <UsersManager />
                <GroupsManager />
            </div>
        </div>

        <div id="panel-settings" role="tabpanel" aria-labelledby="tab-settings" hidden>
            <div class="admin-panels">
                <SettingsManager />
                <PowerBIDiagnosticsCard />
                <DataRefreshManager />
            </div>
        </div>

        <div id="panel-rls" role="tabpanel" aria-labelledby="tab-rls" hidden>
            <div class="admin-panels">
                <RLSManager />
            </div>
        </div>

        <div id="panel-audit" role="tabpanel" aria-labelledby="tab-audit" hidden>
            <div class="admin-panels">
                <AuditLogsPanel />
            </div>
        </div>
    </div>
</template>

<style scoped lang="scss">
.admin-view {
    padding: 2rem;

    h1 {
        margin-bottom: 2rem;
        font-size: 2rem;
        color: var(--cds-text-primary);
    }
}

.admin-panels {
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
    align-items: flex-start;
    margin-top: 2rem;
}

.tab-content {
    padding: 2rem;
}
</style>
