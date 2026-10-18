
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, Observable } from 'rxjs';

import { GestorTurnosService } from './services/gestor-turnos.service';
import {
  Employee,
  Shift,
  ShiftRequest,
  ImportReport
} from './models/gestor-turnos.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly service = inject(GestorTurnosService);
  private readonly fb = inject(FormBuilder);

  readonly employees = signal<Employee[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly editingId = signal<string | null>(null);
  readonly importReport = signal<ImportReport | null>(null);

  readonly filters = this.fb.nonNullable.group({
    from: [''],
    to: ['']
  });

  readonly shiftForm = this.fb.nonNullable.group({
    employeeId: ['', Validators.required],
    date: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadEmployees();
    this.loadShifts();
  }

  loadEmployees(): void {
    this.service.getEmployees().subscribe({
      next: data => this.employees.set(data),
      error: () => this.error.set(
        'No se pudieron cargar los colaboradores.'
      )
    });
  }

  loadShifts(): void {
    const { from, to } = this.filters.getRawValue();

    if (from && to && from > to) {
      this.error.set('El rango de fechas no es válido.');
      return;
    }

    this.error.set('');
    this.loading.set(true);

    this.service.getShifts(from, to)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: data => this.shifts.set(data),
        error: () => this.error.set(
          'No se pudieron cargar los turnos.'
        )
      });
  }

  saveShift(): void {
    if (this.shiftForm.invalid) {
      this.shiftForm.markAllAsTouched();
      this.error.set('Complete los campos obligatorios.');
      return;
    }

    const value = this.shiftForm.getRawValue();

    if (value.endTime <= value.startTime) {
      this.error.set(
        'La hora de fin debe ser posterior a la de inicio.'
      );
      return;
    }

    const request: ShiftRequest = {
      employeeId: value.employeeId,
      date: value.date,
      startTime: value.startTime,
      endTime: value.endTime
    };

    this.saving.set(true);
    this.error.set('');

    
    const editingId = this.editingId();

    const operation: Observable<unknown> = editingId
      ? this.service.updateShift(editingId, request)
      : this.service.createShift(request);

    operation
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.message.set(
            editingId
              ? 'Turno actualizado correctamente.'
              : 'Turno creado correctamente.'
          );

          this.resetForm();
          this.loadShifts();
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(this.getErrorMessage(err));
        }
      });

  }

  editShift(shift: Shift): void {
    this.editingId.set(shift.id);

    this.shiftForm.setValue({
      employeeId: shift.employeeId,
      date: shift.date,
      startTime: shift.startTime.slice(0, 5),
      endTime: shift.endTime.slice(0, 5)
    });

    this.message.set('');
    this.error.set('');
  }

  deleteShift(shift: Shift): void {
    if (!confirm('¿Desea eliminar este turno?'))
      return;

    this.service.deleteShift(shift.id).subscribe({
      next: () => {
        this.message.set('Turno eliminado correctamente.');
        this.loadShifts();
      },
      error: err => this.error.set(this.getErrorMessage(err))
    });
  }

  resetForm(): void {
    this.editingId.set(null);
    this.shiftForm.reset();
  }

  importExcel(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file)
      return;

    this.importReport.set(null);
    this.error.set('');
    this.message.set('');

    this.service.importExcel(file).subscribe({
      next: report => {
        this.importReport.set(report);
        this.message.set(
          `Importación finalizada: ${report.importedRows} correctos, ` +
          `${report.failedRows} rechazados.`
        );
        this.loadShifts();
      },
      error: err => this.error.set(this.getErrorMessage(err))
    });

    input.value = '';
  }

  exportExcel(): void {
    const { from, to } = this.filters.getRawValue();

    if (!from || !to || from > to) {
      this.error.set(
        'Indique un rango de fechas válido para exportar.'
      );
      return;
    }

    this.service.exportExcel(from, to).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = `Turnos_${from}_${to}.xlsx`;
        link.click();

        URL.revokeObjectURL(url);
        this.message.set('Archivo Excel generado.');
      },
      error: err => this.error.set(this.getErrorMessage(err))
    });
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string')
      return error.error;

    if (error.error?.detail)
      return error.error.detail;

    if (error.status === 409)
      return 'El turno se solapa con otro turno existente.';

    if (error.status === 0)
      return 'No se puede conectar con el servidor.';

    return 'Se produjo un error al procesar la solicitud.';
  }
}
