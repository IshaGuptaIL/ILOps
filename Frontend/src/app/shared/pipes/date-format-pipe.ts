import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateFormat',
  standalone: true
})


  export class DateFormatPipe implements PipeTransform {
  
  transform(value: string | Date, format: string = 'MM-DD-YYYY'): string {
    if (!value) return '';
    
    const date = new Date(value);
    
    // Check if valid date
    if (isNaN(date.getTime())) return value.toString();
    
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const year = date.getFullYear();
    
    switch(format) {
      case 'MM-DD-YYYY':
        return `${month}-${day}-${year}`;
      case 'DD-MM-YYYY':
        return `${day}-${month}-${year}`;
      case 'YYYY-MM-DD':
        return `${year}-${month}-${day}`;
      default:
        return `${month}-${day}-${year}`;
    }
  }
}