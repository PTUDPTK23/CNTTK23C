export type Role = 'admin' | 'bidder'
export type AuctionStatus = 'scheduled' | 'active' | 'ended' | 'cancelled'

export interface User {
  id: string
  email: string
  displayName: string
  role: Role
  createdAtUtc: string
}

export interface AuthResponse {
  token: string
  expiresAtUtc: string
  user: User
}

export interface UserProfile extends User {
  totalBids: number
  auctionsJoined: number
  auctionsWon: number
}

export interface Bid {
  id: string
  auctionId: string
  auctionTitle?: string | null
  amount: number
  bidderId: string
  bidderName: string
  placedAtUtc: string
  sequence: number
  isWinning: boolean
}

export interface AuctionSummary {
  id: string
  title: string
  productId: string
  productName: string
  category: string
  imageUrl: string
  startingPrice: number
  currentPrice: number
  minIncrement: number
  minimumNextBid: number
  startTimeUtc: string
  endTimeUtc: string
  status: AuctionStatus
  bidCount: number
  highestBidderName?: string | null
  serverTimeUtc: string
}

export interface Winner {
  userId: string
  displayName: string
  winningAmount: number
  winningBidId?: string | null
}

export interface AuctionDetail extends AuctionSummary {
  productDescription: string
  participantCount: number
  sellerName: string
  createdAtUtc: string
  endedAtUtc?: string | null
  winner?: Winner | null
  recentBids: Bid[]
}

export interface AuctionResult {
  auctionId: string
  title: string
  status: AuctionStatus
  hasWinner: boolean
  winnerId?: string | null
  winnerName?: string | null
  winningAmount?: number | null
  winningBidId?: string | null
  totalBids: number
  endedAtUtc?: string | null
}

export interface PlaceBidResponse {
  bidId: string
  auctionId: string
  amount: number
  currentPrice: number
  minimumNextBid: number
  bidderName: string
  placedAtUtc: string
  message: string
}

export interface Product {
  id: string
  name: string
  description: string
  category: string
  imageUrl: string
  startingPrice: number
  createdAtUtc: string
  updatedAtUtc: string
  auctionCount: number
}

export interface Participant {
  userId: string
  displayName: string
  joinedAtUtc: string
  lastBidAtUtc?: string | null
  bidCount: number
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface AuctionStats {
  totalAuctions: number
  activeAuctions: number
  totalBids: number
  totalBidders: number
}

export interface TimePoint {
  date: string
  count: number
}

export interface StatusCount {
  status: string
  count: number
}

export interface TopAuction {
  id: string
  title: string
  status: AuctionStatus
  currentPrice: number
  bidCount: number
  winnerName?: string | null
}

export interface Dashboard {
  totalProducts: number
  totalAuctions: number
  activeAuctions: number
  scheduledAuctions: number
  endedAuctions: number
  totalUsers: number
  totalBidders: number
  totalBids: number
  totalBidVolume: number
  bidsPerDay: TimePoint[]
  auctionsByStatus: StatusCount[]
  topAuctions: TopAuction[]
  recentBids: Bid[]
  generatedAtUtc: string
}

export interface AdminUser {
  id: string
  email: string
  displayName: string
  role: Role
  isActive: boolean
  createdAtUtc: string
  bidCount: number
}

export interface AuditLog {
  id: string
  eventId: string
  eventType: string
  auctionId?: string | null
  userId?: string | null
  payload: string
  occurredAtUtc: string
  processedAtUtc: string
}

export interface BidAcceptedMessage {
  auctionId: string
  bidId: string
  bidderId: string
  bidderName: string
  amount: number
  previousPrice: number
  currentPrice: number
  minimumNextBid: number
  placedAtUtc: string
  sequence: number
  auctionTitle: string
}

export interface AuctionEndedMessage {
  auctionId: string
  auctionTitle: string
  winnerId?: string | null
  winnerName?: string | null
  winningBidId?: string | null
  winningAmount?: number | null
  totalBids: number
  endedAtUtc: string
}

export interface CreateProductInput {
  name: string
  description: string
  category: string
  imageUrl: string
  startingPrice: number
}

export interface CreateAuctionInput {
  productId: string
  title: string
  startPrice: number
  minIncrement: number
  startTimeUtc: string
  endTimeUtc: string
}
