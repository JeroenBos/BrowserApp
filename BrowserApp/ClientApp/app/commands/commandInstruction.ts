﻿import 'rxjs/add/operator/toPromise';
import { BaseViewModel, BaseComponent } from '../components/base.component';

export class CommandInstruction {
    public readonly commandId: number;
    public readonly viewModelId: number;
    public readonly eventArgs: Exclude<any, null | undefined>;

    public constructor(
        commandId: number,
        viewModel: BaseViewModel | BaseComponent<any> | number,
        eventArgs?: any) {

        this.commandId = commandId;
        this.viewModelId = isNumeric(viewModel)
            ? <number>viewModel
            : (<BaseViewModel | BaseComponent<any>>viewModel).__id;
        this.eventArgs = eventArgs || 'null';
    }
}


function isNumeric(n: any) {
    return !isNaN(parseFloat(n)) && isFinite(n);
}