export default function SkeletonCard() {
  return (
    <div className="card animate-pulse">
      <div className="bg-secondary/20 h-48 rounded-lg mb-4"></div>
      <div className="bg-secondary/20 h-6 rounded w-3/4 mb-2"></div>
      <div className="bg-secondary/20 h-4 rounded w-1/2 mb-3"></div>
      <div className="flex justify-between items-center">
        <div className="bg-secondary/20 h-8 rounded w-24"></div>
        <div className="bg-secondary/20 h-8 rounded w-20"></div>
      </div>
    </div>
  )
}