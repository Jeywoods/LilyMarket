import { apiClient } from './client'

export interface AuctionSummary {
  id: string
  title: string
  category: string
  condition: string
  coverImageUrl: string
  currentHighestBid: number
  startingPrice: number
  buyNowPrice: number | null
  endTime: string
  status: string
  bidCount: number
}

export interface AuctionDetail {
  id: string
  sellerId: string
  sellerName: string
  title: string
  description: string
  category: string
  condition: string
  coverImageUrl: string
  startingPrice: number
  minimumIncrement: number
  buyNowPrice: number | null
  currentHighestBid: number
  currentHighestBidderId: string | null
  startedAt: string
  endTime: string
  status: string
  recentBids: Bid[]
}

export interface Bid {
  bidderId: string
  bidderName: string
  amount: number
  placedAt: string
}

export interface AuctionsResponse {
  items: AuctionSummary[]
  page: number
  pageSize: number
  totalCount: number
  hasNextPage: boolean
  totalPages: number
}

export interface CreateAuctionRequest {
  title: string
  description: string
  category: string
  condition: string
  coverImageUrl: string
  startingPrice: number
  minimumIncrement: number
  buyNowPrice?: number
  endTime: string
}

export interface BidResponse {
  success: boolean
  newHighestBid: number
  message: string
}

export const auctionsApi = {
  getAuctions: (page: number = 1, pageSize: number = 20) =>
    apiClient.get<AuctionsResponse>(`/auctions?page=${page}&pageSize=${pageSize}`),
  
  getAuction: (id: string) =>
    apiClient.get<AuctionDetail>(`/auctions/${id}`),
  
  createAuction: (data: CreateAuctionRequest) =>
    apiClient.post<AuctionDetail>('/auctions', data),
  
  updateAuction: (id: string, data: Partial<CreateAuctionRequest>) =>
    apiClient.put<AuctionDetail>(`/auctions/${id}`, data),
  
  deleteAuction: (id: string) =>
    apiClient.delete(`/auctions/${id}`),
  
  placeBid: (auctionId: string, amount: number) =>
    apiClient.post<BidResponse>(`/auctions/${auctionId}/bids`, { amount }),
}