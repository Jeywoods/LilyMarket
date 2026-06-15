import { useState, useEffect } from 'react'

export interface User {
  userId: string
  displayName: string
  token: string
}

export function useAuth() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    const userId = localStorage.getItem('userId')
    const displayName = localStorage.getItem('displayName')

    if (token && userId && displayName) {
      setUser({ token, userId, displayName })
    }
    setLoading(false)
  }, [])

  const login = (token: string, userId: string, displayName: string) => {
    localStorage.setItem('token', token)
    localStorage.setItem('userId', userId)
    localStorage.setItem('displayName', displayName)
    setUser({ token, userId, displayName })
  }

  const logout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('userId')
    localStorage.removeItem('displayName')
    setUser(null)
  }

  return { user, loading, login, logout }
}