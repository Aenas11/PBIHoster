export interface Page {
    id: number
    title: string
    icon: string
    parentId?: number
    order: number
    roles: string[]
    children?: Page[]
}
