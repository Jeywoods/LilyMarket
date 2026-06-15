import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { auctionsApi, CreateAuctionRequest } from '../api/auctions'
import { useToast } from '../hooks/useToast'

const categories = ['Tech', 'Books', 'Furniture', 'Clothing', 'Art', 'Sports', 'Other']
const conditions = ['New', 'Like New', 'Good', 'Fair']

export default function CreateAuction() {
  const navigate = useNavigate()
  const { showToast } = useToast()
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    category: 'Other',
    condition: 'Good',
    coverImageUrl: '',
    startingPrice: '',
    minimumIncrement: '',
    buyNowPrice: '',
    endTime: '',
  })

  const mutation = useMutation({
    mutationFn: (data: CreateAuctionRequest) => auctionsApi.createAuction(data),
    onSuccess: (data) => {
      showToast('Аукцион успешно создан!', 'success')
      navigate(`/auctions/${data.id}`)
    },
    onError: (error: Error) => {
      showToast(error.message, 'error')
    },
  })

  const validateForm = (): string | null => {
    if (!formData.title.trim()) return 'Введите название'
    if (!formData.description.trim()) return 'Введите описание'
    if (!formData.coverImageUrl.trim()) return 'Добавьте URL изображения'
    if (!formData.startingPrice || parseFloat(formData.startingPrice) <= 0) return 'Введите корректную начальную цену'
    if (!formData.minimumIncrement || parseFloat(formData.minimumIncrement) <= 0) return 'Введите корректный шаг ставки'
    if (!formData.endTime) return 'Выберите дату завершения'
    
    const endDate = new Date(formData.endTime)
    if (endDate <= new Date()) return 'Дата завершения должна быть в будущем'
    
    return null
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    
    const error = validateForm()
    if (error) {
      showToast(error, 'error')
      return
    }

    const data: CreateAuctionRequest = {
      title: formData.title.trim(),
      description: formData.description.trim(),
      category: formData.category,
      condition: formData.condition,
      coverImageUrl: formData.coverImageUrl.trim(),
      startingPrice: parseFloat(formData.startingPrice),
      minimumIncrement: parseFloat(formData.minimumIncrement),
      endTime: new Date(formData.endTime).toISOString(),
    }

    if (formData.buyNowPrice && parseFloat(formData.buyNowPrice) > 0) {
      data.buyNowPrice = parseFloat(formData.buyNowPrice)
    }

    mutation.mutate(data)
  }

  const updateField = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  return (
    <div className="max-w-2xl mx-auto">
      <h2 className="text-2xl font-bold text-accent mb-6">Создать аукцион</h2>
      
      <form onSubmit={handleSubmit} className="space-y-6">
        <div>
          <label className="block text-text mb-2 font-medium">Название *</label>
          <input
            type="text"
            value={formData.title}
            onChange={(e) => updateField('title', e.target.value)}
            className="input-field"
            placeholder="Введите название товара"
            maxLength={200}
          />
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">Описание *</label>
          <textarea
            value={formData.description}
            onChange={(e) => updateField('description', e.target.value)}
            className="input-field min-h-[120px]"
            placeholder="Опишите товар подробнее"
            maxLength={5000}
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-text mb-2 font-medium">Категория</label>
            <select
              value={formData.category}
              onChange={(e) => updateField('category', e.target.value)}
              className="input-field"
            >
              {categories.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-text mb-2 font-medium">Состояние</label>
            <select
              value={formData.condition}
              onChange={(e) => updateField('condition', e.target.value)}
              className="input-field"
            >
              {conditions.map(cond => (
                <option key={cond} value={cond}>{cond}</option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">URL изображения *</label>
          <input
            type="url"
            value={formData.coverImageUrl}
            onChange={(e) => updateField('coverImageUrl', e.target.value)}
            className="input-field"
            placeholder="https://example.com/image.jpg"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div>
            <label className="block text-text mb-2 font-medium">Начальная цена (₽) *</label>
            <input
              type="number"
              value={formData.startingPrice}
              onChange={(e) => updateField('startingPrice', e.target.value)}
              className="input-field"
              min="1"
              step="1"
            />
          </div>

          <div>
            <label className="block text-text mb-2 font-medium">Шаг ставки (₽) *</label>
            <input
              type="number"
              value={formData.minimumIncrement}
              onChange={(e) => updateField('minimumIncrement', e.target.value)}
              className="input-field"
              min="1"
              step="1"
            />
          </div>

          <div>
            <label className="block text-text mb-2 font-medium">Купить сейчас (₽)</label>
            <input
              type="number"
              value={formData.buyNowPrice}
              onChange={(e) => updateField('buyNowPrice', e.target.value)}
              className="input-field"
              min="1"
              step="1"
              placeholder="Необязательно"
            />
          </div>
        </div>

        <div>
          <label className="block text-text mb-2 font-medium">Дата завершения *</label>
          <input
            type="datetime-local"
            value={formData.endTime}
            onChange={(e) => updateField('endTime', e.target.value)}
            className="input-field"
          />
        </div>

        <div className="flex gap-4">
          <button
            type="submit"
            disabled={mutation.isPending}
            className="btn-primary flex-1"
          >
            {mutation.isPending ? 'Создание...' : 'Создать аукцион'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/auctions')}
            className="btn-secondary flex-1"
          >
            Отмена
          </button>
        </div>
      </form>
    </div>
  )
}