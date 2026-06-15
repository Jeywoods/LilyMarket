import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'

export function useSignalR(auctionId: string | null) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  const connect = useCallback(async () => {
    if (!auctionId) return

    const token = localStorage.getItem('token')
    if (!token) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/auction?access_token=${token}`)
      .withAutomaticReconnect()
      .build()

    connection.on('BidPlaced', (data) => {
      window.dispatchEvent(new CustomEvent('bidPlaced', { detail: data }))
    })

    connection.on('Outbid', (data) => {
      window.dispatchEvent(new CustomEvent('outbid', { detail: data }))
    })

    connection.on('AuctionEnded', (data) => {
      window.dispatchEvent(new CustomEvent('auctionEnded', { detail: data }))
    })

    connection.on('AuctionEndingSoon', (data) => {
      window.dispatchEvent(new CustomEvent('auctionEndingSoon', { detail: data }))
    })

    try {
      await connection.start()
      await connection.invoke('JoinAuction', auctionId)
      connectionRef.current = connection
    } catch (err) {
      console.error('SignalR connection error:', err)
    }
  }, [auctionId])

  useEffect(() => {
    connect()

    return () => {
      if (connectionRef.current) {
        connectionRef.current.invoke('LeaveAuction', auctionId)
        connectionRef.current.stop()
      }
    }
  }, [connect, auctionId])

  return connectionRef
}