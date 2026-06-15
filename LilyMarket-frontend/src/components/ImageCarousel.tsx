import { useState } from 'react'

interface ImageCarouselProps {
  images: string[]
}

export default function ImageCarousel({ images }: ImageCarouselProps) {
  const [currentIndex, setCurrentIndex] = useState(0)

  if (images.length === 0) {
    return (
      <div className="h-64 bg-card rounded-card flex items-center justify-center text-6xl">
        📦
      </div>
    )
  }

  return (
    <div className="relative h-64 sm:h-96 bg-card rounded-card overflow-hidden">
      <img 
        src={images[currentIndex]} 
        alt={`Image ${currentIndex + 1}`}
        className="w-full h-full object-cover"
        onError={(e) => {
          (e.target as HTMLImageElement).style.display = 'none'
        }}
      />
      
      {images.length > 1 && (
        <>
          <button 
            onClick={() => setCurrentIndex(prev => (prev - 1 + images.length) % images.length)}
            className="absolute left-2 top-1/2 -translate-y-1/2 bg-primary/80 text-text rounded-full w-10 h-10 flex items-center justify-center hover:bg-primary"
          >
            ←
          </button>
          <button 
            onClick={() => setCurrentIndex(prev => (prev + 1) % images.length)}
            className="absolute right-2 top-1/2 -translate-y-1/2 bg-primary/80 text-text rounded-full w-10 h-10 flex items-center justify-center hover:bg-primary"
          >
            →
          </button>
          <div className="absolute bottom-2 left-1/2 -translate-x-1/2 flex gap-1">
            {images.map((_, index) => (
              <button
                key={index}
                onClick={() => setCurrentIndex(index)}
                className={`w-2 h-2 rounded-full transition-colors ${
                  index === currentIndex ? 'bg-accent' : 'bg-secondary/50'
                }`}
              />
            ))}
          </div>
        </>
      )}
    </div>
  )
}