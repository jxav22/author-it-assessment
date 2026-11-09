import type { CreateTopicRequest, Topic, UpdateTopicRequest } from '../types/api'
import { apiFetch } from './client'

export function listTopics(): Promise<Topic[]> {
  return apiFetch<Topic[]>('/topics')
}

export function getTopic(id: number): Promise<Topic> {
  return apiFetch<Topic>(`/topics/${id}`)
}

export function createTopic(payload: CreateTopicRequest): Promise<Topic> {
  return apiFetch<Topic>('/topics', {
    method: 'POST',
    body: payload,
  })
}

export function updateTopic(
  id: number,
  payload: UpdateTopicRequest,
): Promise<Topic> {
  return apiFetch<Topic>(`/topics/${id}`, {
    method: 'PUT',
    body: payload,
  })
}
