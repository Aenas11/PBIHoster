import { api } from './api'

export interface CommentItem {
  id: number
  pageId: number
  parentId: number | null
  username: string
  content: string
  mentions: string[]
  createdAt: string
  updatedAt: string | null
}

export interface CreateCommentRequest {
  content: string
  parentId?: number | null
  mentions?: string[]
}

export interface UpdateCommentRequest {
  content: string
  mentions?: string[]
}

class CommentsService {
  async getByPage(pageId: number): Promise<CommentItem[]> {
    return api.get<CommentItem[]>(`/comments/page/${pageId}`)
  }

  async create(pageId: number, request: CreateCommentRequest): Promise<CommentItem> {
    return api.post<CommentItem>(`/comments/page/${pageId}`, request)
  }

  async update(commentId: number, request: UpdateCommentRequest): Promise<CommentItem> {
    return api.put<CommentItem>(`/comments/${commentId}`, request)
  }

  async remove(commentId: number): Promise<void> {
    await api.delete(`/comments/${commentId}`)
  }
}

export const commentsService = new CommentsService()
