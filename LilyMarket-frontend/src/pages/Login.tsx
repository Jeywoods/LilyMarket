import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { authApi, LoginRequest } from '../api/auth'
import { useAuth } from '../hooks/useAuth'
import { useToast } from '../hooks/useToast'

export default function Login() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const { showToast } = useToast()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const mutation = useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (data) => {
      login(data.token, data.userId, data.displayName)
      showToast('Вход выполнен успешно!', 'success')
      navigate('/auctions')
    },
    onError: (error: Error) => {
      showToast(error.message, 'error')
    },
  })

  const validateForm = (): string | null => {
    if (!email.trim()) return 'Введите email'
    if (!email.endsWith('@sfedu.ru')) return 'Используйте почту @sfedu.ru'
    if (!password) return 'Введите пароль'
    return null
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    
    const error = validateForm()
    if (error) {
      showToast(error, 'error')
      return
    }

    mutation.mutate({ email, password })
  }

  return (
    <div className="max-w-md mx-auto">
      <h2 className="text-2xl font-bold text-accent mb-6 text-center">Вход в Lily Market</h2>
      
      <form onSubmit={handleSubmit} className="card space-y-4">
        <div>
          <label className="block text-text mb-2 font-medium">Email @sfedu.ru</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="input-field"
            placeholder="student@sfedu.ru"
          />
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">Пароль</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="input-field"
            placeholder="Введите пароль"
          />
        </div>

        <button
          type="submit"
          disabled={mutation.isPending}
          className="btn-primary w-full"
        >
          {mutation.isPending ? 'Вход...' : 'Войти'}
        </button>

        <p className="text-center text-secondary">
          Нет аккаунта?{' '}
          <Link to="/auth/register" className="text-accent hover:underline">
            Зарегистрироваться
          </Link>
        </p>
      </form>
    </div>
  )
}