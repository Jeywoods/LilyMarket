import { Link } from 'react-router-dom'
import { AuctionSummary } from '../api/auctions'

const categoryIcons: Record<string, string> = {
  'Tech': '💻',
  'Books': '📚',
  'Furniture': '🪑',
  'Clothing': '👕',
  'Art': '🎨',
  'Sports': '🏋️',
  'Other': '📦',
}

const conditionColors: Record<string, string> = {
  'New': 'bg-success/20 text-success',
  'Like New': 'bg-blue-500/20 text-blue-400',
  'Good': 'bg-yellow-500/20 text-yellow-400',
  'Fair': 'bg-orange-500/20 text-orange-400',
}

export default function AuctionCard({ auction }: { auction: AuctionSummary }) {
  const currentPrice = auction.currentHighestBid || auction.startingPrice
  const timeLeft = new Date(auction.endTime).getTime() - Date.now()
  const hoursLeft = Math.floor(timeLeft / (1000 * 60 * 60))
  const isEnding = hoursLeft < 1 && auction.status === 'Active'

  return (
    <Link to={`/auctions/${auction.id}`} className="card block animate-fadeIn">
      <div className="relative h-48 bg-secondary/20 rounded-lg mb-4 overflow-hidden">
        {auction.coverImageUrl ? (
          <img 
            src={auction.coverImageUrl} 
            alt={auction.title}
            className="w-full h-full object-cover"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none'
            }}
          />
        ) : (
          <div className="flex items-center justify-center h-full text-4xl">
            {categoryIcons[auction.category] || '📦'}
          </div>
        )}
        <div className="absolute top-2 right-2 flex gap-1">
          <span className={`px-2 py-1 rounded-full text-xs font-medium ${conditionColors[auction.condition] || 'bg-secondary/20 text-secondary'}`}>
            {auction.condition}
          </span>
        </div>
      </div>

      <div className="space-y-2">
        <h3 className="text-text font-semibold text-lg truncate">{auction.title}</h3>
        
        <div className="flex items-center gap-2 text-secondary text-sm">
          <span>{categoryIcons[auction.category]}</span>
          <span>{auction.category}</span>
          <span>•</span>
          <span>{auction.bidCount} ставок</span>
        </div>

        <div className="flex justify-between items-center">
          <div>
            <p className="text-accent text-xl font-bold">{currentPrice} ₽</p>
            {auction.buyNowPrice && (
              <p className="text-secondary text-xs">Купить сейчас: {auction.buyNowPrice} ₽</p>
            )}
          </div>
          
          <div className={`text-right ${isEnding ? 'text-error' : 'text-secondary'}`}>
            <p className="text-sm font-medium">
              {auction.status === 'Sold' ? 'Продано' :
               auction.status === 'Ended' ? 'Завершён' :
               isEnding ? 'Заканчивается' :
               `${hoursLeft}ч`}
            </p>
          </div>
        </div>
      </div>
    </Link>
  )
}