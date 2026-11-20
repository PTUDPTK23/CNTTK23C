import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi, tokenStore, toApiError } from '../lib/api'
import type { UserProfile } from '../lib/types'

interface AuthApi {
  user: UserProfile | null
  initializing: boolean
  isAuthenticated: boolean
  isAdmin: boolean
  login: (email: string, password: string) => Promise<UserProfile>
  register: (email: string, password: string, displayName: string) => Promise<UserProfile>
  logout: () => Promise<void>
  refresh: () => Promise<void>
}

const AuthContext = createContext<AuthApi | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null)
  const [initializing, setInitializing] = useState(true)

  const refresh = useCallback(async () => {
    if (!tokenStore.get()) {
      setUser(null)
      return
    }

    try {
      const profile = await authApi.me()
      setUser(profile)
    } catch (error) {
      const apiError = toApiError(error)
      if (apiError.status === 401) {
        tokenStore.clear()
        setUser(null)
      }
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        await refresh()
      } finally {
        if (!cancelled) setInitializing(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [refresh])

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login(email, password)
    tokenStore.set(response.token)
    setUser({ ...response.user, totalBids: 0, auctionsJoined: 0, auctionsWon: 0 })
    try {
      const profile = await authApi.me()
      setUser(profile)
      return profile
    } catch {
      return { ...response.user, totalBids: 0, auctionsJoined: 0, auctionsWon: 0 }
    }
  }, [])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const response = await authApi.register(email, password, displayName)
    tokenStore.set(response.token)
    const profile: UserProfile = { ...response.user, totalBids: 0, auctionsJoined: 0, auctionsWon: 0 }
    setUser(profile)
    return profile
  }, [])

  const logout = useCallback(async () => {
    try {
      await authApi.logout()
    } catch {
      // Stateless JWT: a failed call must still log the client out.
    }
    tokenStore.clear()
    setUser(null)
  }, [])

  const value = useMemo<AuthApi>(
    () => ({
      user,
      initializing,
      isAuthenticated: Boolean(user),
      isAdmin: user?.role === 'admin',
      login,
      register,
      logout,
      refresh
    }),
    [user, initializing, login, register, logout, refresh]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthApi {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
