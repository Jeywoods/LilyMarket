import { Bid } from '../api/auctions'

interface BidHistoryProps {
  bids: Bid[]
}

export default function BidHistory({ bids }: BidHistoryProps) {
  if (bids.length === 0) {
    return (
      <div className="text-center py-8 text-secondary">
        Пока нет ставок. Будьте первым!
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {bids.map((bid, index) => (
        <div key={index} className="flex justify-between items-center p-3 bg-primary/30 rounded-lg">
          <div>
            <p className="text-text font-medium">{bid.bidderName}</p>
            <p className="text-secondary text-sm">
              {new Date(bid.placedAt).toLocaleString('ru-RU')}
            </p>
          </div>
          <p className="text-accent font-bold text-lg">{bid.amount} ₽</p>
        </div>
      ))}
    </div>
  )
}