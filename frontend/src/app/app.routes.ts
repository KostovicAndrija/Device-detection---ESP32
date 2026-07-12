import { Routes } from '@angular/router';

import { MainLayout } from './layouts/main-layout/main-layout';

export const routes: Routes = [
  {
    path: '',
    component: MainLayout,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/pages/dashboard/dashboard').then(m => m.Dashboard)
      },
      {
        path: 'floor-map',
        loadComponent: () => import('./features/floor-map/pages/floor-map/floor-map').then(m => m.FloorMap)
      },
      {
        path: 'devices',
        loadComponent: () => import('./features/devices/pages/devices/devices').then(m => m.Devices)
      },
      {
        path: 'alerts',
        loadComponent: () => import('./features/alerts/pages/alerts/alerts').then(m => m.Alerts)
      },
      {
        path: 'whitelist',
        loadComponent: () => import('./features/whitelist/pages/whitelist/whitelist').then(m => m.Whitelist)
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/pages/reports/reports').then(m => m.Reports)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
