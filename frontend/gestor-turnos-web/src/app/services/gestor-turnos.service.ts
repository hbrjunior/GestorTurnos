
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  Employee,
  Shift,
  ShiftRequest,
  ImportReport
} from '../models/gestor-turnos.models';

@Injectable({
  providedIn: 'root'
})
export class GestorTurnosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getEmployees() {
    return this.http.get<Employee[]>(
      `${this.baseUrl}/employees`
    );
  }

  getShifts(from?: string, to?: string) {
    let params = new HttpParams();

    if (from)
      params = params.set('from', from);

    if (to)
      params = params.set('to', to);

    return this.http.get<Shift[]>(
      `${this.baseUrl}/shifts`,
      { params }
    );
  }

  createShift(request: ShiftRequest) {
    return this.http.post<Shift>(
      `${this.baseUrl}/shifts`,
      request
    );
  }

  updateShift(id: string, request: ShiftRequest) {
    return this.http.put<void>(
      `${this.baseUrl}/shifts/${id}`,
      request
    );
  }

  deleteShift(id: string) {
    return this.http.delete<void>(
      `${this.baseUrl}/shifts/${id}`
    );
  }

  importExcel(file: File) {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ImportReport>(
      `${this.baseUrl}/shifts/import`,
      formData
    );
  }

  exportExcel(from: string, to: string) {
    const params = new HttpParams()
      .set('from', from)
      .set('to', to);

    return this.http.get(
      `${this.baseUrl}/shifts/export`,
      {
        params,
        responseType: 'blob'
      }
    );
  }
}
