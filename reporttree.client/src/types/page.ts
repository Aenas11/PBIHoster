export interface Page {
    id: number
    title: string
    icon: string
    parentId?: number
    order: number
    isPublic: boolean
    sensitivityLabel?: 'Public' | 'Internal' | 'Confidential' | 'Restricted'
    allowedUsers: string[]
    allowedGroups: string[]
    layout?: string
    isDemo?: boolean
    children?: Page[]
}
