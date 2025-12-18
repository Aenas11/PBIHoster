export interface Page {
    id: number
    title: string
    icon: string
    parentId?: number
    order: number
    isPublic: boolean
    allowedUsers: string[]
    allowedGroups: string[]
    layout?: string
    children?: Page[]
}
