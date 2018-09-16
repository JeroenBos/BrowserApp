import 'rxjs/add/operator/toPromise';
import { BaseViewModel, BaseComponent } from '../base.component';
import { CommandArgs } from './inputTypes';

export class CommandInstruction {
    public readonly commandName: string;
    public readonly viewModelId: number;
    public readonly eventArgs: Exclude<any, null | undefined>;

    public constructor(
        commandName: string,
        viewModel: BaseViewModel | BaseComponent<any> | number,
        eventArgs?: CommandArgs) {

        this.commandName = commandName;
        this.viewModelId = isNumeric(viewModel)
            ? <number>viewModel
            : (<BaseViewModel | BaseComponent<any>>viewModel).__id;
        this.eventArgs = eventArgs || 'null';
    }
}


function isNumeric(n: any) {
    return !isNaN(parseFloat(n)) && isFinite(n);
}