import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { auctionsApi } from '../api/auctions'
import { useAuth } from '../hooks/useAuth'
import { useSignalR } from '../hooks/useSignalR'
import { useToast } from '../hooks/useToast'
import ImageCarousel from '../components/ImageCarousel'
import BidHistory from '../components/BidHistory'

export default function AuctionDetail() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const { showToast } = useToast()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [bidAmount, setBidAmount] = useState('')
  const [activeTab, setActiveTab] = useState<'description' | 'history'>('description')
  const [timeLeft, setTimeLeft] = useState('')

  const { data: auction, isLoading, error } = useQuery({
    queryKey: ['auction', id],
    queryFn: () => auctionsApi.getAuction(id!),
    enabled: !!id,
  })

  useSignalR(id || null)

  const bidMutation = useMutation({
    mutationFn: (amount: number) => auctionsApi.placeBid(id!, amount),
    onSuccess: (data) => {
      showToast(data.message || 'Ставка принята!', 'success')
      queryClient.invalidateQueries({ queryKey: ['auction', id] })
      setBidAmount('')
    },
    onError: (error: Error) => {
      showToast(error.message, 'error')
    },
  })

  useEffect(() => {
    const handleBidPlaced = () => {
      queryClient.invalidateQueries({ queryKey: ['auction', id] })
    }
    
    const handleOutbid = () => {
      showToast('Вас перебили!', 'error')
    }
    
    const handleAuctionEnded = (event: Event) => {
      const data = (event as CustomEvent).detail
      showToast(`Аукцион завершён! Победитель: ${data.winnerName}`, 'success')
      queryClient.invalidateQueries({ queryKey: ['auction', id] })
    }
    
    const handleAuctionEndingSoon = () => {
      showToast('Аукцион скоро завершится!', 'success')
    }

    window.addEventListener('bidPlaced', handleBidPlaced)
    window.addEventListener('outbid', handleOutbid)
    window.addEventListener('auctionEnded', handleAuctionEnded)
    window.addEventListener('auctionEndingSoon', handleAuctionEndingSoon)

    return () => {
      window.removeEventListener('bidPlaced', handleBidPlaced)
      window.removeEventListener('outbid', handleOutbid)
      window.removeEventListener('auctionEnded', handleAuctionEnded)
      window.removeEventListener('auctionEndingSoon', handleAuctionEndingSoon)
    }
  }, [id, queryClient, showToast])

  useEffect(() => {
    if (!auction) return

    const timer = setInterval(() => {
      const now = Date.now()
      const end = new Date(auction.endTime).getTime()
      const diff = end - now

      if (diff <= 0) {
        setTimeLeft('Завершён')
        clearInterval(timer)
        return
      }

      const hours = Math.floor(diff / (1000 * 60 * 60))
      const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
      const seconds = Math.floor((diff % (1000 * 60)) / 1000)
      setTimeLeft(`${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`)
    }, 1000)

    return () => clearInterval(timer)
  }, [auction])

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-6">
        <div className="h-64 bg-card rounded-card"></div>
        <div className="h-8 bg-card rounded w-3/4"></div>
        <div className="h-4 bg-card rounded w-1/2"></div>
      </div>
    )
  }

  if (error || !auction) {
    return (
      <div className="text-center py-12">
        <p className="text-error text-lg">Аукцион не найден</p>
      </div>
    )
  }

  const isEnded = auction.status === 'Ended' || auction.status === 'Sold'
  const isOwner = user?.userId === auction.sellerId
  const currentPrice = auction.currentHighestBid || auction.startingPrice
  const hoursLeft = timeLeft.includes(':') ? parseInt(timeLeft.split(':')[0]) : 0

  const handleBid = () => {
    if (!user) {
      navigate('/auth/login')
      return
    }

    const amount = parseFloat(bidAmount)
    if (isNaN(amount) || amount <= 0) {
      showToast('Введите корректную сумму', 'error')
      return
    }

    if (amount <= currentPrice) {
      showToast('Ставка должна быть выше текущей', 'error')
      return
    }

    bidMutation.mutate(amount)
  }

  const handleBuyNow = () => {
    if (!user) {
      navigate('/auth/login')
      return
    }

    if (auction.buyNowPrice) {
      bidMutation.mutate(auction.buyNowPrice)
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <button 
        onClick={() => navigate('/auctions')}
        className="text-secondary hover:text-text transition-colors"
      >
        ← Назад к аукционам
      </button>

      <ImageCarousel images={auction.coverImageUrl ? [auction.coverImageUrl] : []} />

      <div className="space-y-4">
        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-text">{auction.title}</h1>
            <p className="text-secondary mt-1">Продавец: {auction.sellerName}</p>
          </div>

          <div className={`text-right ${hoursLeft < 1 && !isEnded ? 'text-error' : 'text-accent'}`}>
            <p className="text-sm text-secondary">
              {isEnded ? 'Статус' : 'До конца'}
            </p>
            <p className="text-2xl font-bold">
              {isEnded ? auction.status === 'Sold' ? 'Продано' : 'Завершён' : timeLeft}
            </p>
          </div>
        </div>

        <div className="card space-y-4">
          <div className="flex justify-between items-center">
            <div>
              <p className="text-secondary text-sm">Текущая цена</p>
              <p className="text-accent text-3xl font-bold">{currentPrice} ₽</p>
            </div>
            {auction.buyNowPrice && !isEnded && (
              <button
                onClick={handleBuyNow}
                className="btn-secondary"
                disabled={isOwner || bidMutation.isPending}
              >
                Купить сейчас: {auction.buyNowPrice} ₽
              </button>
            )}
          </div>

          {!isEnded && !isOwner && (
            <div className="flex gap-3">
              <input
                type="number"
                value={bidAmount}
                onChange={(e) => setBidAmount(e.target.value)}
                placeholder={`Мин. ставка: ${currentPrice + auction.minimumIncrement} ₽`}
                className="input-field flex-1"
                min={currentPrice + auction.minimumIncrement}
                step={auction.minimumIncrement}
              />
              <button
                onClick={handleBid}
                disabled={bidMutation.isPending}
                className="btn-primary"
              >
                {bidMutation.isPending ? '...' : 'Сделать ставку'}
              </button>
            </div>
          )}

          {isOwner && (
            <p className="text-secondary text-sm">Это ваш аукцион</p>
          )}
        </div>

        <div>
          <div className="flex border-b border-secondary/20">
            <button
              onClick={() => setActiveTab('description')}
              className={`px-4 py-3 text-sm font-medium transition-colors ${
                activeTab === 'description'
                  ? 'text-accent border-b-2 border-accent'
                  : 'text-secondary hover:text-text'
              }`}
            >
              Описание
            </button>
            <button
              onClick={() => setActiveTab('history')}
              className={`px-4 py-3 text-sm font-medium transition-colors ${
                activeTab === 'history'
                  ? 'text-accent border-b-2 border-accent'
                  : 'text-secondary hover:text-text'
              }`}
            >
              История ставок ({auction.recentBids.length})
            </button>
          </div>

          <div className="mt-4">
            {activeTab === 'description' ? (
              <div className="space-y-4">
                <p className="text-text whitespace-pre-wrap">{auction.description}</p>
                <div className="flex flex-wrap gap-4 text-sm">
                  <div className="bg-card rounded-card px-3 py-2">
                    <span className="text-secondary">Категория: </span>
                    <span className="text-text">{auction.category}</span>
                  </div>
                  <div className="bg-card rounded-card px-3 py-2">
                    <span className="text-secondary">Состояние: </span>
                    <span className="text-text">{auction.condition}</span>
                  </div>
                  <div className="bg-card rounded-card px-3 py-2">
                    <span className="text-secondary">Начальная цена: </span>
                    <span className="text-text">{auction.startingPrice} ₽</span>
                  </div>
                  <div className="bg-card rounded-card px-3 py-2">
                    <span className="text-secondary">Мин. шаг: </span>
                    <span className="text-text">{auction.minimumIncrement} ₽</span>
                  </div>
                </div>
              </div>
            ) : (
              <BidHistory bids={auction.recentBids} />
            )}
          </div>
        </div>
      </div>
    </div>
  )
}