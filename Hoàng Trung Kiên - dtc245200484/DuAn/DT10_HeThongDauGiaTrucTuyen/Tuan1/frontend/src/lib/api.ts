import axios, { AxiosError } from 'axios'
import type {
  AdminUser,
  AuctionDetail,
  AuctionResult,
  AuctionStats,
  AuctionSummary,
  AuditLog,
  AuthResponse,
  Bid,
  CreateAuctionInput,
  CreateProductInput,
  Dashboard,
  Paged,
  Participant,
  PlaceBidResponse,
  Product,
  UserProfile
} from './types'

export const API_BASE = (import.meta.env.VITE_API_URL ?? 'http://localhost:5100').replace(/\/+$/, '')
export const HUB_URL = import.meta.env.VITE_HUB_URL ?? `${API_BASE}/hubs/auction`
const TOKEN_KEY = 'aurelia.token'

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY)
}

export const api = axios.create({
  baseURL: API_BASE,
  timeout: 20000,
  headers: { 'Content-Type': 'application/json' }
})

api.interceptors.request.use((config) => {
  const token = tokenStore.get()
  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly details?: unknown

  constructor(status: number, code: string, message: string, details?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.details = details
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error

  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{
      code?: string
      message?: string
      title?: string
      detail?: string
      details?: unknown
      errors?: Record<string, string[]>
    }>

    if (!axiosError.response) {
      return new ApiError(0, 'NETWORK_ERROR',
        'Không thể kết nối tới máy chủ đấu giá. Vui lòng kiểm tra API đã chạy hay chưa.')
    }

    const data = axiosError.response.data
    const validationMessage = data?.errors
      ? Object.values(data.errors).flat().join(' ')
      : undefined

    return new ApiError(
      axiosError.response.status,
      data?.code ?? (axiosError.response.status === 401 ? 'UNAUTHORIZED' : 'REQUEST_FAILED'),
      data?.message ?? data?.detail ?? data?.title ?? validationMessage ?? 'Yêu cầu không thành công.',
      data?.details
    )
  }

  return new ApiError(0, 'UNKNOWN_ERROR', error instanceof Error ? error.message : 'Đã xảy ra lỗi không mong muốn.')
}

export function mediaUrl(path?: string | null): string {
  if (!path) return ''
  if (/^https?:\/\//i.test(path) || path.startsWith('data:')) return path
  return `${API_BASE}${path.startsWith('/') ? '' : '/'}${path}`
}

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/api/auth/login', { email, password }).then((r) => r.data),
  register: (email: string, password: string, displayName: string) =>
    api.post<AuthResponse>('/api/auth/register', { email, password, displayName }).then((r) => r.data),
  logout: () => api.post('/api/auth/logout').then((r) => r.data),
  me: () => api.get<UserProfile>('/api/users/me').then((r) => r.data)
}

export const auctionsApi = {
  list: (params: {
    status?: string
    category?: string
    q?: string
    sort?: string
    page?: number
    pageSize?: number
  }) => api.get<Paged<AuctionSummary>>('/api/auctions', { params }).then((r) => r.data),

  stats: () => api.get<AuctionStats>('/api/auctions/stats').then((r) => r.data),

  get: (id: string) => api.get<AuctionDetail>(`/api/auctions/${id}`).then((r) => r.data),

  bids: (id: string, page = 1, pageSize = 50) =>
    api.get<Paged<Bid>>(`/api/auctions/${id}/bids`, { params: { page, pageSize } }).then((r) => r.data),

  participants: (id: string) =>
    api.get<Participant[]>(`/api/auctions/${id}/participants`).then((r) => r.data),

  result: (id: string) => api.get<AuctionResult>(`/api/auctions/${id}/result`).then((r) => r.data),

  placeBid: (id: string, amount: number) =>
    api.post<PlaceBidResponse>(`/api/auctions/${id}/bids`, { amount }).then((r) => r.data),

  create: (input: CreateAuctionInput) =>
    api.post<AuctionDetail>('/api/auctions', input).then((r) => r.data),

  end: (id: string) => api.post<AuctionResult>(`/api/auctions/${id}/end`).then((r) => r.data)
}

export const productsApi = {
  list: (params: { q?: string; category?: string; page?: number; pageSize?: number }) =>
    api.get<Paged<Product>>('/api/products', { params }).then((r) => r.data),
  categories: () => api.get<string[]>('/api/products/categories').then((r) => r.data),
  create: (input: CreateProductInput) =>
    api.post<Product>('/api/products', input).then((r) => r.data),
  update: (id: string, input: CreateProductInput) =>
    api.put<Product>(`/api/products/${id}`, input).then((r) => r.data),
  remove: (id: string) => api.delete(`/api/products/${id}`).then((r) => r.data)
}

export const usersApi = {
  myBids: (page = 1, pageSize = 20) =>
    api.get<Paged<Bid>>('/api/users/me/bids', { params: { page, pageSize } }).then((r) => r.data),
  myAuctions: () => api.get<AuctionSummary[]>('/api/users/me/auctions').then((r) => r.data),
  myWins: () => api.get<AuctionSummary[]>('/api/users/me/wins').then((r) => r.data)
}

export const adminApi = {
  dashboard: () => api.get<Dashboard>('/api/admin/dashboard').then((r) => r.data),
  users: (params: { q?: string; page?: number; pageSize?: number }) =>
    api.get<Paged<AdminUser>>('/api/admin/users', { params }).then((r) => r.data),
  updateRole: (id: string, role: 'admin' | 'bidder') =>
    api.put<AdminUser>(`/api/admin/users/${id}/role`, { role }).then((r) => r.data),
  updateStatus: (id: string, isActive: boolean) =>
    api.put<AdminUser>(`/api/admin/users/${id}/status`, { isActive }).then((r) => r.data),
  auditLogs: (params: { eventType?: string; page?: number; pageSize?: number }) =>
    api.get<Paged<AuditLog>>('/api/admin/audit-logs', { params }).then((r) => r.data)
}
