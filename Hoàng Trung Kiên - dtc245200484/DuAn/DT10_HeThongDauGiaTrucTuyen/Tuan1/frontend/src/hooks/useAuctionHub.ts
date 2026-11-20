import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { HUB_URL, tokenStore } from '../lib/api'
import type { AuctionEndedMessage, BidAcceptedMessage } from '../lib/types'

export type HubStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

interface HubHandlers {
  onBidAccepted?: (message: BidAcceptedMessage) => void
  onAuctionEnded?: (message: AuctionEndedMessage) => void
  onConnected?: (serverTimeUtc: string) => void
}

export function useAuctionHub(
  auctionId: string | undefined,
  handlers: HubHandlers,
  enabled = true
): HubStatus {
  const [status, setStatus] = useState<HubStatus>('connecting')
  const handlersRef = useRef(handlers)
  handlersRef.current = handlers

  useEffect(() => {
    if (!auctionId || !enabled) {
      setStatus('disconnected')
      return
    }

    let disposed = false
    let retryTimer: number | undefined

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => tokenStore.get() ?? ''
      })
      .withAutomaticReconnect([0, 1000, 3000, 6000, 12000])
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('BidAccepted', (message: BidAcceptedMessage) => {
      handlersRef.current.onBidAccepted?.(message)
    })
    connection.on('AuctionEnded', (message: AuctionEndedMessage) => {
      handlersRef.current.onAuctionEnded?.(message)
    })
    connection.on('Connected', (payload: { serverTimeUtc: string }) => {
      handlersRef.current.onConnected?.(payload.serverTimeUtc)
    })

    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(async () => {
      setStatus('connected')
      try {
        await connection.invoke('JoinAuction', auctionId)
      } catch {
        // Group rejoin is retried on the next reconnect.
      }
    })
    connection.onclose(() => {
      if (!disposed) setStatus('disconnected')
    })

    const start = async () => {
      try {
        await connection.start()
        if (disposed) return
        setStatus('connected')
        await connection.invoke('JoinAuction', auctionId)
      } catch {
        if (disposed) return
        setStatus('disconnected')
        retryTimer = window.setTimeout(start, 4000)
      }
    }

    void start()

    return () => {
      disposed = true
      if (retryTimer) window.clearTimeout(retryTimer)
      connection.invoke('LeaveAuction', auctionId).catch(() => undefined)
      connection.stop().catch(() => undefined)
    }
  }, [auctionId, enabled])

  return status
}

export function useServerOffset(serverTimeUtc?: string): number {
  const [offset, setOffset] = useState(0)

  useEffect(() => {
    if (!serverTimeUtc) return
    const server = new Date(serverTimeUtc).getTime()
    if (!Number.isNaN(server)) {
      setOffset(server - Date.now())
    }
  }, [serverTimeUtc])

  return offset
}
