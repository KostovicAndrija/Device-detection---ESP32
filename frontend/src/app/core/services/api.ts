import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class Api {
  readonly restBaseUrl = '/api';
  readonly signalRHubUrl = '/hubs/monitoring';
}
