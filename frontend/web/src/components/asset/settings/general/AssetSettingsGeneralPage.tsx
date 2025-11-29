import { FormikTextInput } from "../../../ui/form/text-input/FormikTextInput";
import { Form, Formik } from "formik";
import { RenameAssetFormValues, useRenameAsset } from "./useRenameAsset";
import { FormattedMessage } from "react-intl";
import { DeleteAssetModal } from "./DeleteAssetModal";
import { Icon } from "../../../ui/icon/Icon";
import { faCheck, faTerminal } from "@fortawesome/free-solid-svg-icons";
import { LoadingIndicator } from "../../../ui/loading-indicator/LoadingIndicator";
import { useCurrentAsset } from "@navtrack/shared/hooks/current/useCurrentAsset";
import { Card } from "../../../ui/card/Card";
import { CardBody } from "../../../ui/card/CardBody";
import { Heading } from "../../../ui/heading/Heading";
import { Button } from "../../../ui/button/Button";
import { GpsCommandsModal } from "../../commands/GpsCommandsModal";
import { useState } from "react";
import { useAuthorize } from "@navtrack/shared/hooks/current/useAuthorize";

export function AssetSettingsGeneralPage() {
  const renameAsset = useRenameAsset();
  const currentAsset = useCurrentAsset();
  const authorize = useAuthorize();
  const [commandsModalOpen, setCommandsModalOpen] = useState(false);

  return (
    <>
      {currentAsset && (
        <Card>
          <CardBody>
            <div>
              <Heading type="h2">
                <FormattedMessage id="assets.settings.general" />
              </Heading>
              <div className="mt-4">
                <Formik<RenameAssetFormValues>
                  initialValues={{
                    name: currentAsset.data?.name ?? "",
                    chasisNumber: currentAsset.data?.chasisNumber ?? ""
                  }}
                  onSubmit={(values, formikHelpers) =>
                    renameAsset.submit(values, formikHelpers)
                  }
                  validationSchema={renameAsset.validationSchema}
                  enableReinitialize>
                  {() => (
                    <Form className="grid grid-cols-12 gap-6">
                      <div className="col-span-7 space-y-4">
                        <FormikTextInput
                          name="name"
                          label="generic.name"
                          loading={currentAsset.data === undefined}
                        />
                        <FormikTextInput
                          name="chasisNumber"
                          label="generic.chasis-number"
                          loading={currentAsset.data === undefined}
                        />
                        <div className="flex items-center">
                          <Button
                            color="secondary"
                            type="submit"
                            size="md"
                            disabled={currentAsset.data === undefined}>
                            <FormattedMessage id="assets.settings.general.save" />
                          </Button>
                          <div className="ml-2 w-4">
                            {renameAsset.loading && <LoadingIndicator />}
                            {renameAsset.showSuccess && (
                              <Icon
                                icon={faCheck}
                                className="text-green-600"
                              />
                            )}
                          </div>
                        </div>
                      </div>
                    </Form>
                  )}
                </Formik>
              </div>
            </div>
            {authorize.canSendCommands() && (
              <div className="mt-6">
                <Heading type="h2">
                  <FormattedMessage id="gps.commands.title" />
                </Heading>
                <p className="mt-2 text-sm text-gray-500">
                  <FormattedMessage id="gps.commands.description" />
                </p>
                <div className="mt-4 text-right">
                  <Button
                    color="primary"
                    icon={faTerminal}
                    onClick={() => setCommandsModalOpen(true)}>
                    <FormattedMessage id="gps.commands.open" />
                  </Button>
                </div>
              </div>
            )}
            <div className="mt-6">
              <Heading type="h2">
                <FormattedMessage id="assets.settings.general.delete-asset" />
              </Heading>
              <p className="mt-2 text-sm text-gray-500">
                <FormattedMessage id="assets.settings.general.delete-asset.info" />
              </p>
              <div className="mt-4 text-right">
                <DeleteAssetModal />
              </div>
            </div>
          </CardBody>
        </Card>
      )}
      {currentAsset.data && (
        <GpsCommandsModal
          asset={currentAsset.data}
          open={commandsModalOpen}
          close={() => setCommandsModalOpen(false)}
        />
      )}
    </>
  );
}
