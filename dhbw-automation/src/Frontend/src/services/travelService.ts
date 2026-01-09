import api from './api';

export interface TrainConnectionRequest {
  from: string;
  to: string;
  dateTime?: string;
  maxConnections?: number;
}

export interface Journey {
  from: string;
  to: string;
  departure: string;
  arrival: string;
  duration: string;
  transfers: number;
  legs: Leg[];
  cancelled?: boolean;
  delay?: number;
}

export interface Leg {
  from: string;
  to: string;
  departure: string;
  arrival: string;
  line?: string;
  direction?: string;
  platform?: string;
  delay?: number;
  cancelled?: boolean;
}

export interface TrainConnectionResponse {
  journeys: Journey[];
  requestedAt: string;
}

export const travelService = {
  /**
   * Ruft Zugverbindungen ab
   */
  async getConnections(request: TrainConnectionRequest): Promise<TrainConnectionResponse> {
    const response = await api.post('/travel/connections', request);
    return response.data;
  },

  /**
   * Ruft die Standard-Verbindung (Laupheim West - Ravensburg) ab
   */
  async getDefaultConnections(maxConnections: number = 5): Promise<TrainConnectionResponse> {
    const response = await api.get(
      `/travel/connections/default?maxConnections=${maxConnections}`
    );
    return response.data;
  },
};
