export interface AppUser {
    id?: number
    username: string
    roles?: string[]
    groups?: string[]
    password?: string // Only for creation/update
}

export interface Group {
    id?: number
    name: string
    description?: string
}

export interface UpsertUserRequest {
    username: string
    password?: string
    roles?: string[]
    groups?: string[]
}
