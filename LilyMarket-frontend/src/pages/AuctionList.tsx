import { useEffect, useRef, useCallback } from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'
import { auctionsApi } from '../api/auctions'
import AuctionCard from '../components/AuctionCard'
import SkeletonCard from '../components/SkeletonCard'

export default function AuctionList() {
  const observerRef = useRef<IntersectionObserver | null>(null)
  
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    error,
  } = useInfiniteQuery({
    queryKey: ['auctions'],
    queryFn: ({ pageParam = 1 }) => auctionsApi.getAuctions(pageParam, 20),
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.page + 1 : undefined,
    initialPageParam: 1,
  })

  const lastAuctionRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (isFetchingNextPage) return
      if (observerRef.current) observerRef.current.disconnect()
      
      observerRef.current = new IntersectionObserver(entries => {
        if (entries[0].isIntersecting && hasNextPage) {
          fetchNextPage()
        }
      })
      
      if (node) observerRef.current.observe(node)
    },
    [isFetchingNextPage, hasNextPage, fetchNextPage]
  )

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-error text-lg">Ошибка загрузки аукционов</p>
      </div>
    )
  }

  return (
    <div>
      <h2 className="text-2xl font-bold text-accent mb-6">Активные аукционы</h2>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {isLoading ? (
          Array.from({ length: 8 }).map((_, i) => <SkeletonCard key={i} />)
        ) : (
          data?.pages.map((page) =>
            page.items.map((auction, index) => {
              if (index === page.items.length - 1) {
                return (
                  <div ref={lastAuctionRef} key={auction.id}>
                    <AuctionCard auction={auction} />
                  </div>
                )
              }
              return <AuctionCard key={auction.id} auction={auction} />
            })
          )
        )}
      </div>
      
      {isFetchingNextPage && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 mt-4">
          {Array.from({ length: 4 }).map((_, i) => <SkeletonCard key={i} />)}
        </div>
      )}
      
      {data?.pages[0]?.items.length === 0 && !isLoading && (
        <div className="text-center py-12 text-secondary">
          <p className="text-xl">Пока нет активных аукционов</p>
          <p className="mt-2">Создайте первый аукцион!</p>
        </div>
      )}
    </div>
  )
}