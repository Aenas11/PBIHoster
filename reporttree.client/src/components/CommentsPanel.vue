<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { commentsService, type CommentItem } from '../services/commentsService'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/textarea/index.js'
import '@carbon/web-components/es/components/loading/index.js'
import '@carbon/web-components/es/components/tag/index.js'

const props = defineProps<{
  open: boolean
  pageId: number
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const authStore = useAuthStore()
const toast = useToastStore()

const loading = ref(false)
const comments = ref<CommentItem[]>([])
const newComment = ref('')
const replyParentId = ref<number | null>(null)
const editId = ref<number | null>(null)
const editContent = ref('')

const canPost = computed(() => authStore.isAuthenticated)
const rootComments = computed(() => comments.value.filter(c => c.parentId == null))

function requestClose() {
  emit('close')
}

function onKeyDown(event: KeyboardEvent) {
  if (event.key === 'Escape' && props.open) {
    requestClose()
  }
}

function mentionsFromContent(content: string): string[] {
  const matches = content.match(/@([A-Za-z0-9._-]+)/g) ?? []
  return Array.from(new Set(matches.map(x => x.slice(1))))
}

function repliesFor(parentId: number): CommentItem[] {
  return comments.value.filter(c => c.parentId === parentId)
}

function formatDate(value: string) {
  return new Date(value).toLocaleString()
}

function startReply(parentId: number) {
  if (!authStore.isAuthenticated) {
    toast.warning('Sign in required', 'You must be signed in to reply')
    return
  }
  replyParentId.value = parentId
  newComment.value = ''
}

function cancelReply() {
  replyParentId.value = null
}

function startEdit(comment: CommentItem) {
  editId.value = comment.id
  editContent.value = comment.content
}

function cancelEdit() {
  editId.value = null
  editContent.value = ''
}

async function loadComments() {
  if (!props.pageId || !props.open) {
    return
  }

  loading.value = true
  try {
    comments.value = await commentsService.getByPage(props.pageId)
  } catch (error) {
    console.error('Failed to load comments', error)
    toast.error('Error', 'Failed to load comments')
  } finally {
    loading.value = false
  }
}

async function submitComment() {
  const content = newComment.value.trim()
  if (!content) {
    return
  }

  try {
    const created = await commentsService.create(props.pageId, {
      content,
      parentId: replyParentId.value,
      mentions: mentionsFromContent(content)
    })

    comments.value.push(created)
    comments.value.sort((a, b) => Date.parse(a.createdAt) - Date.parse(b.createdAt))
    newComment.value = ''
    replyParentId.value = null
  } catch (error) {
    console.error('Failed to create comment', error)
    toast.error('Error', 'Failed to post comment')
  }
}

async function saveEdit(commentId: number) {
  const content = editContent.value.trim()
  if (!content) {
    return
  }

  try {
    const updated = await commentsService.update(commentId, {
      content,
      mentions: mentionsFromContent(content)
    })

    const index = comments.value.findIndex(c => c.id === commentId)
    if (index >= 0) {
      comments.value[index] = updated
    }

    cancelEdit()
  } catch (error) {
    console.error('Failed to update comment', error)
    toast.error('Error', 'Failed to update comment')
  }
}

async function deleteComment(commentId: number) {
  try {
    await commentsService.remove(commentId)
    comments.value = comments.value.filter(c => c.id !== commentId && c.parentId !== commentId)
  } catch (error) {
    console.error('Failed to delete comment', error)
    toast.error('Error', 'Failed to delete comment')
  }
}

watch(() => props.open, (isOpen) => {
  if (isOpen) {
    void loadComments()
  }
})

watch(() => props.pageId, () => {
  if (props.open) {
    void loadComments()
  }
})

onMounted(() => {
  window.addEventListener('keydown', onKeyDown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeyDown)
})
</script>

<template>
  <transition name="comments-slide">
    <div v-if="open" class="comments-shell">
      <button class="comments-backdrop" type="button" aria-label="Close comments panel" @click="requestClose"></button>
      <aside class="comments-panel" aria-label="Comments panel">
        <header class="comments-header">
          <h3>Comments</h3>
          <button type="button" class="close-button" @click="requestClose">Close</button>
        </header>

      <div class="composer" v-if="canPost">
        <div class="reply-label" v-if="replyParentId !== null">
          Replying to comment #{{ replyParentId }}
          <cds-button kind="ghost" size="sm" @click="cancelReply">Cancel</cds-button>
        </div>
        <cds-textarea
          label="Add comment"
          :value="newComment"
          @input="newComment = ($event.target as HTMLTextAreaElement).value"
          placeholder="Write a comment... Use @username to mention someone"
        ></cds-textarea>
        <div class="composer-actions">
          <cds-button kind="primary" size="sm" @click="submitComment">Post</cds-button>
        </div>
      </div>

      <p v-else class="auth-hint">Sign in to post comments.</p>

      <div v-if="loading" class="loading">
        <cds-loading></cds-loading>
      </div>

      <div v-else class="thread-list">
        <article v-for="root in rootComments" :key="root.id" class="comment-card">
          <div class="meta">
            <strong>{{ root.username }}</strong>
            <span>{{ formatDate(root.createdAt) }}</span>
            <cds-tag v-if="root.updatedAt" type="gray" size="sm">edited</cds-tag>
          </div>

          <template v-if="editId === root.id">
            <cds-textarea
              label="Edit comment"
              :value="editContent"
              @input="editContent = ($event.target as HTMLTextAreaElement).value"
            ></cds-textarea>
            <div class="row-actions">
              <cds-button kind="primary" size="sm" @click="saveEdit(root.id)">Save</cds-button>
              <cds-button kind="ghost" size="sm" @click="cancelEdit">Cancel</cds-button>
            </div>
          </template>
          <p v-else class="content">{{ root.content }}</p>

          <div class="row-actions" v-if="authStore.isAuthenticated">
            <cds-button kind="ghost" size="sm" @click="startReply(root.id)">Reply</cds-button>
            <cds-button kind="ghost" size="sm" @click="startEdit(root)">Edit</cds-button>
            <cds-button kind="ghost" size="sm" @click="deleteComment(root.id)">Delete</cds-button>
          </div>

          <div class="replies" v-if="repliesFor(root.id).length > 0">
            <article v-for="reply in repliesFor(root.id)" :key="reply.id" class="reply-card">
              <div class="meta">
                <strong>{{ reply.username }}</strong>
                <span>{{ formatDate(reply.createdAt) }}</span>
              </div>
              <template v-if="editId === reply.id">
                <cds-textarea
                  label="Edit reply"
                  :value="editContent"
                  @input="editContent = ($event.target as HTMLTextAreaElement).value"
                ></cds-textarea>
                <div class="row-actions">
                  <cds-button kind="primary" size="sm" @click="saveEdit(reply.id)">Save</cds-button>
                  <cds-button kind="ghost" size="sm" @click="cancelEdit">Cancel</cds-button>
                </div>
              </template>
              <p v-else class="content">{{ reply.content }}</p>
              <div class="row-actions" v-if="authStore.isAuthenticated">
                <cds-button kind="ghost" size="sm" @click="startEdit(reply)">Edit</cds-button>
                <cds-button kind="ghost" size="sm" @click="deleteComment(reply.id)">Delete</cds-button>
              </div>
            </article>
          </div>
        </article>

        <p v-if="!loading && comments.length === 0" class="empty">No comments yet.</p>
      </div>
      </aside>
    </div>
  </transition>
</template>

<style scoped>
.comments-shell {
  position: fixed;
  inset: 0;
  z-index: 1300;
}

.comments-backdrop {
  position: absolute;
  inset: 0;
  border: none;
  background: rgba(0, 0, 0, 0.3);
  cursor: pointer;
}

.comments-panel {
  position: absolute;
  top: 0;
  right: 0;
  width: min(420px, 92vw);
  height: 100vh;
  background: var(--cds-layer);
  border-left: 1px solid var(--cds-border-subtle);
  z-index: 1300;
  display: flex;
  flex-direction: column;
  padding: 1rem;
  overflow: hidden;
}

.close-button {
  border: 1px solid var(--cds-border-subtle);
  background: transparent;
  color: var(--cds-text-primary);
  padding: 0.3rem 0.6rem;
  cursor: pointer;
}

.comments-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.composer {
  margin-top: 0.5rem;
  border: 1px solid var(--cds-border-subtle);
  padding: 0.75rem;
}

.composer-actions {
  margin-top: 0.6rem;
  display: flex;
  justify-content: flex-end;
}

.auth-hint {
  margin: 0.75rem 0;
  color: var(--cds-text-secondary);
}

.thread-list {
  margin-top: 0.75rem;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.comment-card,
.reply-card {
  border: 1px solid var(--cds-border-subtle);
  padding: 0.75rem;
  background: var(--cds-layer-accent);
}

.reply-card {
  margin-top: 0.5rem;
  margin-left: 1rem;
}

.meta {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  font-size: 0.8rem;
  color: var(--cds-text-secondary);
}

.content {
  white-space: pre-wrap;
  margin: 0.5rem 0;
}

.row-actions {
  display: flex;
  gap: 0.35rem;
  justify-content: flex-end;
}

.reply-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.25rem;
  font-size: 0.8rem;
  color: var(--cds-text-secondary);
}

.empty,
.loading {
  text-align: center;
  color: var(--cds-text-secondary);
  padding: 1rem;
}

.comments-slide-enter-active,
.comments-slide-leave-active {
  pointer-events: none;
}

.comments-slide-enter-active .comments-panel,
.comments-slide-leave-active .comments-panel {
  transition: transform 0.22s cubic-bezier(0.2, 0, 0, 1);
}

.comments-slide-enter-from .comments-panel,
.comments-slide-leave-to .comments-panel {
  transform: translateX(100%);
}

.comments-slide-enter-active .comments-backdrop,
.comments-slide-leave-active .comments-backdrop {
  transition: opacity 0.12s ease;
}

.comments-slide-enter-from .comments-backdrop,
.comments-slide-leave-to .comments-backdrop {
  opacity: 0;
}
</style>
