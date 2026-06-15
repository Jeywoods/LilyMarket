import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { authApi, RegisterRequest } from '../api/auth'
import { useAuth } from '../hooks/useAuth'
import { useToast } from '../hooks/useToast'

export default function Register() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const { showToast } = useToast()
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    displayName: '',
  })

  const mutation = useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
    onSuccess: (data) => {
      login(data.token, data.userId, data.displayName)
      showToast('Регистрация успешна!', 'success')
      navigate('/auctions')
    },
    onError: (error: Error) => {
      showToast(error.message, 'error')
    },
  })

  const validateForm = (): string | null => {
    if (!formData.email.trim()) return 'Введите email'
    if (!formData.email.endsWith('@sfedu.ru')) return 'Используйте почту @sfedu.ru'
    if (!formData.displayName.trim()) return 'Введите имя'
    if (formData.displayName.trim().length < 2) return 'Имя должно быть не менее 2 символов'
    if (!formData.password) return 'Введите пароль'
    if (formData.password.length < 6) return 'Пароль должен быть не менее 6 символов'
    if (formData.password !== formData.confirmPassword) return 'Пароли не совпадают'
    return null
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    
    const error = validateForm()
    if (error) {
      showToast(error, 'error')
      return
    }

    mutation.mutate({
      email: formData.email,
      password: formData.password,
      displayName: formData.displayName.trim(),
    })
  }

  const updateField = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  return (
    <div className="max-w-md mx-auto">
      <h2 className="text-2xl font-bold text-accent mb-6 text-center">Регистрация в Lily Market</h2>
      
      <form onSubmit={handleSubmit} className="card space-y-4">
        <div>
          <label className="block text-text mb-2 font-medium">Email @sfedu.ru</label>
          <input
            type="email"
            value={formData.email}
            onChange={(e) => updateField('email', e.target.value)}
            className="input-field"
            placeholder="student@sfedu.ru"
          />
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">Имя</label>
          <input
            type="text"
            value={formData.displayName}
            onChange={(e) => updateField('displayName', e.target.value)}
            className="input-field"
            placeholder="Ваше имя"
          />
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">Пароль</label>
          <input
            type="password"
            value={formData.password}
            onChange={(e) => updateField('password', e.target.value)}
            className="input-field"
            placeholder="Минимум 6 символов"
          />
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">Подтверждение пароля</label>
          <input
            type="password"
            value={formData.confirmPassword}
            onChange={(e) => updateField('confirmPassword', e.target.value)}
            className="input-field"
            placeholder="Повторите пароль"
          />
        </div>

        <button
          type="submit"
          disabled={mutation.isPending}
          className="btn-primary w-full"
        >
          {mutation.isPending ? 'Регистрация...' : 'Зарегистрироваться'}
        </button>

        <p className="text-center text-secondary">
          Уже есть аккаунт?{' '}
          <Link to="/auth/login" className="text-accent hover:underline">
            Войти
          </Link>
        </p>
      </form>
    </div>
  )
}