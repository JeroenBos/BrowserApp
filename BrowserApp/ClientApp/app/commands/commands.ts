import 'rxjs/add/operator/toPromise';
import { BaseViewModel, BaseComponent } from '../components/base.component';
import { CommandInstruction } from './commandInstruction';
import { CanonicalInputBinding } from './inputBindingParser';
import { Booleanable, ConditionAST } from './ConditionAST'
import { InputEvent, CommandArgs } from './inputTypes';

export interface ClientsideOptimizationCommand {
    canExecute(sender: BaseViewModel, e: CommandArgs): OptimizationCanExecute;
    execute(sender: BaseViewModel, e: CommandArgs): void;
}

export enum OptimizationCanExecute {
    /** Indicates the associated command cannot be executed. 
     * The input that triggered this command will not be consumed. */
    False,
    /** Indicates the client side optimization cannot execute, but the non-optimized form (server side) of the command can be executed.
     * The input that trigged this command will be consumed. */
    ServersideOnly,
    /** Indicates no serverside command should be executed, only this client side 'optimization'. 
     * The input that trigged this command will be consumed. */
    ClientsideOnly,
    /** Indicates the associated command can be executed. Both serverside and clientside. 
     * The input that trigged this command will be consumed. */
    True,
}

export interface CommandViewModel extends BaseViewModel {
    name: string;
    condition: string | undefined;
}
export class Command extends BaseComponent<CommandViewModel> {
    public get id() {
        return super.__id;
    }
    public get name() {
        return this.viewModel.name;
    }
    public condition(flags: Map<string, boolean>): Booleanable {
        return this.viewModel.condition === undefined
            ? { toBoolean: () => true }
            : ConditionAST.parse(this.viewModel.condition, flags);
    }
    public constructor(
        viewModel: CommandViewModel,
        /** If the client side event args is undefined, that means the command was triggered from code rather then UI event. 
         The returned value is information given to the server for command execution, and also
         given to the clientside command execution, if any, alongside the input event, if any*/
        public readonly getEventArgs: (clientsideEventArgs: InputEvent | undefined) => CommandArgs = () => undefined,
        public clientsideOptimization?: ClientsideOptimizationCommand) {
        super(undefined, viewModel)
    }

    public toInstruction(sender: BaseViewModel, eventArgs: CommandArgs): CommandInstruction {
        const serverSideEventArgs = this.getEventArgs == null ? null : this.getEventArgs(eventArgs);
        return new CommandInstruction(this.id, sender, serverSideEventArgs);
    }
}
export class CommandBinding {
    public constructor(
        public readonly condition: Booleanable,
        public readonly input: CanonicalInputBinding) {
    }
}
export class CommandBindingWithCommandName {
    public constructor(public readonly commandName: string,
        public readonly condition: Booleanable,
        public readonly input: CanonicalInputBinding) {
    }
}