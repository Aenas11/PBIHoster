import { api } from './api'

export interface PageVersionItem {
  id: number
  pageId: number
  layout: string
  changedBy: string
  changedAt: string
  changeDescription: string
}

class PageVersionsService {
  async getByPage(pageId: number, take = 20): Promise<PageVersionItem[]> {
    return api.get<PageVersionItem[]>(`/pages/${pageId}/versions?take=${take}`)
  }

  async rollback(pageId: number, versionId: number, changeDescription?: string): Promise<void> {
    await api.post(`/pages/${pageId}/versions/${versionId}/rollback`, {
      changeDescription: changeDescription ?? ''
    })
  }
}

export const pageVersionsService = new PageVersionsService()
